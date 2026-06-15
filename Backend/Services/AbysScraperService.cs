using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;

namespace ControlLaboratorio.API.Services
{
    /// <summary>
    /// Datos de un lector extraídos del sistema AbsysNet de la biblioteca URP.
    /// </summary>
    public class AbysLectorDto
    {
        public string CodigoUniversitario { get; set; } = "";
        public string Nombres { get; set; } = "";
        public string ApellidoPaterno { get; set; } = "";
        public string ApellidoMaterno { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Carrera { get; set; } = "";
        public string CorreoInstitucional { get; set; } = "";
    }

    /// <summary>
    /// Servicio que se conecta al sistema AbsysNet de la Biblioteca URP,
    /// inicia sesión con las credenciales configuradas y busca los datos
    /// de un alumno a partir del código de barras de su carnet universitario.
    /// </summary>
    public class AbysScraperService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AbysScraperService> _logger;
        private const string BaseUrl = "https://biblioteca.urp.edu.pe";

        public AbysScraperService(IConfiguration config, ILogger<AbysScraperService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Busca un lector en AbsysNet por su código de carnet universitario.
        /// Retorna null si no se encuentra o si ocurre un error.
        /// </summary>
        public async Task<AbysLectorDto?> BuscarPorCodigoAsync(string codigoUniversitario)
        {
            // Handler que acepta cookies (necesario para mantener la sesión CGI)
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer()
            };

            using var http = new HttpClient(handler);
            http.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36");
            http.Timeout = TimeSpan.FromSeconds(20);

            try
            {
                var usuario = _config["AbysNet:Usuario"] ?? "medicina";
                var password = _config["AbysNet:Password"] ?? "biblioteca1";

                // ── PASO 1: Login ─────────────────────────────────────────────────────────
                _logger.LogInformation("AbysNet: Iniciando sesión con usuario '{U}'", usuario);
                var loginBody = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["USER"] = usuario,
                    ["PASS"] = password,
                    ["DH"]   = "/abnet"
                });

                var loginResp = await http.PostAsync($"{BaseUrl}/abnet/abnetcl.exe", loginBody);
                var loginHtml = await loginResp.Content.ReadAsStringAsync();

                // Extraer el path UD (sesión temporal post-login)
                var matchUD = Regex.Match(loginHtml, @"abnetcl\.exe(/X\d+/UD\w+)");
                if (!matchUD.Success)
                {
                    _logger.LogWarning("AbysNet: No se encontró UD path tras el login. HTML={H}", loginHtml[..Math.Min(300, loginHtml.Length)]);
                    return null;
                }
                var udPath = matchUD.Groups[1].Value;

                // ── PASO 2: Obtener el ID de sesión definitivo ────────────────────────────
                var r2 = await http.GetAsync($"{BaseUrl}/abnet/abnetcl.exe{udPath}?ACC=1111");
                var html2 = await r2.Content.ReadAsStringAsync();

                var matchID = Regex.Match(html2, @"abnetcl\.exe(/X\d+/ID\w+/)");
                if (!matchID.Success)
                {
                    _logger.LogWarning("AbysNet: No se encontró ID de sesión definitivo.");
                    return null;
                }
                var sid = matchID.Groups[1].Value;
                _logger.LogInformation("AbysNet: Sesión obtenida: {S}", sid);

                // ── PASO 3: Buscar el lector por código de barras ─────────────────────────
                var searchUrl = $"{BaseUrl}/abnet/abnetcl.exe{sid}NT119" +
                                $"?ACC=110&NV=1&AV=1&TBV=2&SF=NUM_LECTOR&SFT=CLAVE_BARRAS&TQ={Uri.EscapeDataString(codigoUniversitario)}";

                var r3 = await http.GetAsync(searchUrl);
                var html3 = await r3.Content.ReadAsStringAsync();

                // AbsysNet devuelve un WpGetFrameset que apunta al NT con los resultados
                var matchNT = Regex.Match(html3, @"abnetcl\.exe(/X\d+/ID\w+/NT\d+)");
                if (!matchNT.Success)
                {
                    _logger.LogWarning("AbysNet: No se encontró NT path en resultado de búsqueda para código {C}", codigoUniversitario);
                    return null;
                }
                var ntPath = matchNT.Groups[1].Value;

                // ── PASO 4: Obtener la ficha del lector directamente desde el NT de búsqueda ──
                // ntPath es el NT que contiene el resultado (ej: NT358)
                // ACC=104 = MOSTRAR la ficha del primer registro encontrado
                var fichaUrl = $"{BaseUrl}/abnet/abnetcl.exe{ntPath}?ACC=104";
                _logger.LogInformation("AbysNet: Obteniendo ficha: {U}", fichaUrl);
                var r4 = await http.GetAsync(fichaUrl);

                // AbsysNet responde en ISO-8859-1; decodificar correctamente
                var bytes = await r4.Content.ReadAsByteArrayAsync();
                var fichaHtml = Encoding.Latin1.GetString(bytes);
                
                _logger.LogInformation("AbysNet: Ficha HTML (500 chars): {H}", fichaHtml[..Math.Min(500, fichaHtml.Length)]);

                return ParsearFicha(fichaHtml, codigoUniversitario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AbysNet: Error al consultar código {C}", codigoUniversitario);
                return null;
            }
        }

        // ── Extracción de datos del HTML de la ficha ──────────────────────────────────────
        private static AbysLectorDto? ParsearFicha(string html, string codigoBuscado)
        {
            // Si AbsysNet devolvió 0 registros (NREC=0), no hay datos
            if (Regex.IsMatch(html, @"name=""NREC""\s+value=""0""", RegexOptions.IgnoreCase))
                return null;
            // Extraer valor de un <input name="xxx" value="yyy">
            static string GetField(string h, string name)
            {
                var m = Regex.Match(h,
                    $@"name=""{Regex.Escape(name)}""\s+[^>]*value=""([^""]*)""",
                    RegexOptions.IgnoreCase);
                if (!m.Success)
                    m = Regex.Match(h,
                        $@"value=""([^""]*)""\s+[^>]*name=""{Regex.Escape(name)}""",
                        RegexOptions.IgnoreCase);
                return m.Groups[1].Value.Trim();
            }

            var nroLector = GetField(html, "lenlec");

            // Si AbsysNet devolvió un lector diferente al buscado, rechazar
            if (!string.IsNullOrWhiteSpace(nroLector) &&
                nroLector != codigoBuscado)
            {
                // Verificamos también que el DNI exista (registro válido)
                var dniCheck = GetField(html, "leddni");
                if (string.IsNullOrWhiteSpace(dniCheck)) return null;
            }

            var apellidos   = GetField(html, "leapel"); // "QUELLO YAPU"
            var nombres     = GetField(html, "lenomb"); // "CLAUDIO FERNANDO"
            var iniciales   = GetField(html, "leinic"); // "Q.Y."
            var dni         = GetField(html, "leddni"); // "72493906"
            var cod1        = GetField(html, "lecol1"); // Ej: "00001"
            var cod2        = GetField(html, "lecol2"); // Ej: "06" o texto carrera

            if (string.IsNullOrWhiteSpace(nroLector) || string.IsNullOrWhiteSpace(nombres))
                return null;

            // Separar apellidos: AbsysNet los guarda juntos (PATERNO MATERNO)
            var (apPat, apMat) = SepararApellidos(apellidos, iniciales);

            // Mapear código de carrera al nombre legible
            var carrera = MapearCarrera(cod2);

            // Correo institucional estándar URP
            var correo = string.IsNullOrWhiteSpace(nroLector)
                ? ""
                : $"{nroLector}@urp.edu.pe";

            return new AbysLectorDto
            {
                CodigoUniversitario = nroLector,
                Nombres             = ToTitleCase(nombres),
                ApellidoPaterno     = ToTitleCase(apPat),
                ApellidoMaterno     = ToTitleCase(apMat),
                Dni                 = dni,
                Carrera             = carrera,
                CorreoInstitucional = correo
            };
        }

        /// <summary>
        /// Separa "QUELLO YAPU" en PaternO="QUELLO" y MaternO="YAPU"
        /// usando las iniciales "Q.Y." como guía cuando hay ambigüedad.
        /// </summary>
        private static (string pat, string mat) SepararApellidos(string apellidos, string iniciales)
        {
            if (string.IsNullOrWhiteSpace(apellidos)) return ("", "");

            var partes = apellidos.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return (partes[0], "");
            if (partes.Length == 2) return (partes[0], partes[1]);

            // 3+ palabras: usar las iniciales para determinar el corte
            // Iniciales tienen formato "P.M." donde P=1ª letra paterno, M=1ª letra materno
            if (!string.IsNullOrWhiteSpace(iniciales))
            {
                var letras = iniciales.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (letras.Length >= 2)
                {
                    char inicialMaterno = letras[1][0];
                    // Buscar desde la derecha la primera parte que empiece con la inicial materna
                    for (int i = partes.Length - 1; i >= 1; i--)
                    {
                        if (char.ToUpper(partes[i][0]) == char.ToUpper(inicialMaterno))
                        {
                            var pat = string.Join(" ", partes[..i]);
                            var mat = string.Join(" ", partes[i..]);
                            return (pat, mat);
                        }
                    }
                }
            }

            // Fallback: primera palabra = paterno, resto = materno
            return (partes[0], string.Join(" ", partes[1..]));
        }

        /// <summary>
        /// Mapea el código de carrera de AbsysNet al nombre legible.
        /// Los códigos son configurados por la biblioteca URP en su sistema.
        /// </summary>
        private static string MapearCarrera(string codigo)
        {
            // Si el campo ya contiene texto (no solo número), devolverlo directamente
            if (!string.IsNullOrWhiteSpace(codigo) && !Regex.IsMatch(codigo, @"^\d+$"))
                return codigo;

            return codigo switch
            {
                "01" or "1"  => "Ingeniería Civil",
                "02" or "2"  => "Ingeniería Electrónica",
                "03" or "3"  => "Ingeniería Industrial",
                "04" or "4"  or "06" or "6" => "Ingeniería Informática",
                "05" or "5"  => "Ingeniería Mecatrónica",
                "07" or "7"  => "Administración y Gerencia",
                "08" or "8"  => "Contabilidad y Finanzas",
                "09" or "9"  => "Derecho y Ciencia Política",
                "10"         => "Medicina Humana",
                "11"         => "Psicología",
                "12"         => "Arquitectura y Urbanismo",
                "13"         => "Economía",
                "14"         => "Turismo, Hotelería y Gastronomía",
                "15"         => "Biología",
                "16"         => "Medicina Veterinaria",
                "17"         => "Traducción e Interpretación",
                "18"         => "Marketing Global y Administración Comercial",
                "19"         => "Administración de Negocios Globales",
                _            => "" // Si no se reconoce, dejar en blanco para que el usuario seleccione
            };
        }

        private static string ToTitleCase(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo
                .ToTitleCase(s.ToLower());
        }
    }
}
