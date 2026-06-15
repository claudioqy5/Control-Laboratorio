using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace ControlLaboratorio.API.Services
{
    /// <summary>
    /// Datos de un lector extraídos del sistema AbsysNet de la biblioteca URP.
    /// </summary>
    public class AbysLectorDto
    {
        public string CodigoUniversitario { get; set; } = "";
        public string Nombres             { get; set; } = "";
        public string ApellidoPaterno     { get; set; } = "";
        public string ApellidoMaterno     { get; set; } = "";
        public string Dni                 { get; set; } = "";
        public string Carrera             { get; set; } = "";
        public string CorreoInstitucional { get; set; } = "";
    }

    /// <summary>
    /// Servicio que usa Playwright (headless Chromium) para conectarse al sistema
    /// AbsysNet de la Biblioteca URP y extraer datos del lector por código de barras.
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
            var usuario  = _config["AbysNet:Usuario"]  ?? "medicina";
            var password = _config["AbysNet:Password"] ?? "biblioteca1";

            _logger.LogInformation("AbysNet: Iniciando búsqueda headless para código {C}", codigoUniversitario);

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args     = new[] { "--disable-dev-shm-usage", "--no-sandbox" }
                });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    IgnoreHTTPSErrors = true,
                });
                var page = await context.NewPageAsync();

                // ── PASO 1: Login ─────────────────────────────────────────────────────────
                _logger.LogInformation("AbysNet: Navegando al login...");
                await page.GotoAsync($"{BaseUrl}/abnet/inicio.htm",
                    new PageGotoOptions { Timeout = 20000 });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 15000 });

                // Completar el formulario de login
                await page.FillAsync("input[name='USER']", usuario);
                await page.FillAsync("input[name='PASS']", password);
                await page.ClickAsync("input[type='submit']");

                // Esperar que cargue el frameset principal
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 15000 });
                await Task.Delay(2000); // Dar tiempo extra para que los frames carguen

                _logger.LogInformation("AbysNet: Login OK. URL: {U}", page.Url);

                // ── PASO 2: Acceder al módulo de Gestión de Lectores ─────────────────────
                // El menú está en el frame AbxMenu
                var menuFrame = page.Frame("AbxMenu");
                if (menuFrame == null)
                {
                    _logger.LogWarning("AbysNet: No se encontró el frame AbxMenu");
                    return null;
                }

                // Esperar a que el menú cargue y hacer click en "Gestión de lectores"
                await menuFrame.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new FrameWaitForLoadStateOptions { Timeout = 10000 });

                // Expandir y hacer click usando JavaScript para evitar problemas con acentos y elementos ocultos
                try
                {
                    // Ejecutar script en el frame del menú para encontrar y hacer click en "Gestión de lectores"
                    await menuFrame.EvaluateAsync(@"() => {
                        const links = Array.from(document.querySelectorAll('a'));
                        
                        // Encontrar el nodo principal 'Lectores' y hacerle click si tiene hijos
                        const lectoresMain = links.find(a => a.textContent.trim() === 'Lectores');
                        if (lectoresMain) lectoresMain.click();

                        // Esperar un momento y encontrar 'Gestión de lectores'
                        setTimeout(() => {
                            const linkGestion = Array.from(document.querySelectorAll('a'))
                                .find(a => a.textContent.includes('estión') && a.textContent.includes('lectores'));
                            if (linkGestion) linkGestion.click();
                        }, 500);
                    }");
                    await Task.Delay(2000); // Esperar que la acción tome efecto
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AbysNet: Advertencia al ejecutar JS de navegación: {E}", ex.Message);
                }

                // Esperar que cargue el formulario de búsqueda en AbxMain
                var mainFrame = page.Frame("AbxMain");
                if (mainFrame == null)
                {
                    _logger.LogWarning("AbysNet: No se encontró el frame AbxMain");
                    return null;
                }

                await mainFrame.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new FrameWaitForLoadStateOptions { Timeout = 10000 });
                await Task.Delay(1500);

                var lenlecInput = await mainFrame.QuerySelectorAsync("#lenlec");
                
                // Fallback: Si no se encuentra lenlec, forzar la navegación directa a la URL del formulario
                if (lenlecInput == null)
                {
                    _logger.LogInformation("AbysNet: Campo #lenlec no encontrado por click en menú. Forzando navegación directa.");
                    var matchUrl = Regex.Match(page.Url, @"(/abnet/abnetcl\.exe/X\d+/ID\w+/)");
                    if (matchUrl.Success)
                    {
                        string targetUrl = $"{BaseUrl}{matchUrl.Groups[1].Value}NT1?ACC=110&TB=29";
                        await mainFrame.GotoAsync(targetUrl);
                        await mainFrame.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        await Task.Delay(1500);
                        lenlecInput = await mainFrame.QuerySelectorAsync("#lenlec");
                    }
                }

                // ── PASO 3: Ingresar el código en el campo "Nº lector" ───────────────────
                if (lenlecInput == null)
                {
                    _logger.LogWarning("AbysNet: Definitivamente no se encontró el campo #lenlec en el formulario");
                    return null;
                }

                _logger.LogInformation("AbysNet: Ingresando código {C} en lenlec...", codigoUniversitario);
                await lenlecInput.ClickAsync();
                await lenlecInput.FillAsync(codigoUniversitario);
                await lenlecInput.PressAsync("Tab"); // Tab activa la búsqueda por código
                await Task.Delay(2000); // Esperar que cargue la ficha

                // ── PASO 4: Leer los datos del formulario ────────────────────────────────
                // Obtener el HTML del frame principal con los datos del lector
                var fichaHtml = await mainFrame.ContentAsync();
                _logger.LogInformation("AbysNet: Ficha HTML ({L} chars)", fichaHtml.Length);

                // Parsear los datos del lector
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
            // Si AbsysNet devolvió 0 registros (NREC=0) y el código no aparece, no hay datos
            var nrecMatch = Regex.Match(html, @"name=""NREC""\s+value=""(\d+)""", RegexOptions.IgnoreCase);
            var lenlecMatch = Regex.Match(html, @"name=""lenlec""\s+[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase);
            
            // Si lenlec está vacío o no coincide con el código, no hay datos
            if (!lenlecMatch.Success || string.IsNullOrEmpty(lenlecMatch.Groups[1].Value))
                return null;

            // Extraer valor de un <input name="xxx" value="yyy">
            static string GetField(string h, string name)
            {
                var m = Regex.Match(h,
                    $@"name=""{Regex.Escape(name)}""\s+[^>]*value=""([^""]*)""",
                    RegexOptions.IgnoreCase);
                return m.Success ? m.Groups[1].Value.Trim() : "";
            }

            // Extraer texto de un <td class="..."> que sigue a otro TD con la etiqueta
            static string GetTextAfterLabel(string h, string label)
            {
                // Buscar el texto después de una celda con la etiqueta
                var m = Regex.Match(h,
                    $@"{Regex.Escape(label)}\s*</td>\s*<td[^>]*>\s*([^<]+)",
                    RegexOptions.IgnoreCase);
                return m.Success ? m.Groups[1].Value.Trim() : "";
            }

            var lenlec  = GetField(html, "lenlec");
            var lenomb  = GetField(html, "lenomb");  // Nombre
            var leapel  = GetField(html, "leapel");  // Apellidos
            var leinic  = GetField(html, "leinic");  // Iniciales
            var leddni  = GetField(html, "leddni");  // DNI
            var lemail  = GetField(html, "lemail");  // Email

            // Carrera: lecol2 suele tener el código de carrera con descripción en el combo
            // Buscar el texto seleccionado en el combo lecol2
            var carreraMatch = Regex.Match(html,
                @"name=""lecol2""\s+[^>]*value=""(\d+)"".*?<option\s+value=""\1""\s+selected[^>]*>([^<]+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var carrera = carreraMatch.Success
                ? carreraMatch.Groups[2].Value.Trim()
                : GetField(html, "lecol2");

            // Si no se encontró la carrera en el combo, buscar texto de la sucursal (lecosu)
            if (string.IsNullOrEmpty(carrera))
            {
                // El campo lecol2 puede tener un número: buscar la descripción en el combo JS
                var lecol2Val = GetField(html, "lecol2");
                var sucursalMatch = Regex.Match(html,
                    $@"comboObj\(""lecol2"".*?\[\s*""{Regex.Escape(lecol2Val)}""\s*,\s*""([^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                carrera = sucursalMatch.Success ? sucursalMatch.Groups[1].Value.Trim() : lecol2Val;
            }

            // Separar apellidos (en AbsysNet suelen venir como "APPATERNO APMATERNO")
            var apellidos = leapel.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var apPaterno = apellidos.Length > 0 ? apellidos[0] : "";
            var apMaterno = apellidos.Length > 1 ? apellidos[1] : "";

            return new AbysLectorDto
            {
                CodigoUniversitario = lenlec,
                Nombres             = lenomb,
                ApellidoPaterno     = apPaterno,
                ApellidoMaterno     = apMaterno,
                Dni                 = leddni,
                Carrera             = carrera,
                CorreoInstitucional = lemail
            };
        }
    }
}
