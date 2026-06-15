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
                    var matchUrl = Regex.Match(mainFrame.Url, @"(/abnet/abnetcl\.exe/X\d+/[IU]D\w+/)");
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

                // Cerrar diálogo si estuviera abierto en la página padre para evitar intercepciones de clicks/eventos
                try
                {
                    await page.EvaluateAsync(@"() => {
                        const diag = document.querySelector('dialog');
                        if (diag) {
                            const closeBtn = document.querySelector('#dialog-close');
                            if (closeBtn) closeBtn.click();
                            diag.remove();
                        }
                    }");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("AbysNet: No se pudo cerrar el diálogo modal: {E}", ex.Message);
                }

                _logger.LogInformation("AbysNet: Ingresando código {C} en lenlec...", codigoUniversitario);
                await lenlecInput.FocusAsync();
                await lenlecInput.FillAsync(codigoUniversitario);
                await lenlecInput.PressAsync("Enter"); // Enter activa la búsqueda/submit
                await Task.Delay(4000); // Esperar que cargue la ficha

                // ── PASO 4: Leer los datos del formulario/resultado ──────────────────────
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
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // Helper para obtener el texto después de una celda con etiqueta (para el modo lectura ACC=104)
            Func<string, string> getVal = (label) =>
            {
                var lblNode = doc.DocumentNode.SelectSingleNode($"//td[contains(@class, 'LabelR') and (normalize-space(text())='{label}' or contains(text(), '{label}'))]");
                if (lblNode == null) return "";
                
                var nextTd = lblNode.SelectSingleNode("following-sibling::td[1]");
                if (nextTd != null)
                {
                    // Si contiene celdas de tabla Inp1 internas (útil para Tr./Inic./Nombre)
                    var nestedInpNodes = nextTd.SelectNodes(".//td[contains(@class, 'Inp1')]");
                    if (nestedInpNodes != null && nestedInpNodes.Count > 0)
                    {
                        for (int i = nestedInpNodes.Count - 1; i >= 0; i--)
                        {
                            var txt = nestedInpNodes[i].InnerText.Replace("&nbsp;", " ").Trim();
                            if (!string.IsNullOrEmpty(txt)) return txt;
                        }
                    }

                    var valNode = nextTd.SelectSingleNode(".//span") ?? nextTd;
                    var fontNode = valNode.SelectSingleNode(".//font");
                    if (fontNode != null) return fontNode.InnerText.Trim();
                    return valNode.InnerText.Replace("&nbsp;", " ").Trim();
                }
                return "";
            };

            // Intentar parsear usando el modo lectura (ACC=104)
            var lenlec = getVal("Nº lector");
            var lenomb = getVal("Tr./Inic./Nombre");
            var leapel = getVal("Apellidos");
            var leddni = getVal("DNI");
            var carrera = getVal("Sucursal");
            var lemail = getVal("Correo") ?? getVal("Email") ?? getVal("E-mail");

            // Si los campos de lectura están vacíos, hacer fallback al modo edición (campos input tradicionales)
            if (string.IsNullOrEmpty(lenlec))
            {
                static string GetField(string h, string name)
                {
                    var m = Regex.Match(h,
                        $@"name=""{Regex.Escape(name)}""\s+[^>]*value=""([^""]*)""",
                        RegexOptions.IgnoreCase);
                    return m.Success ? m.Groups[1].Value.Trim() : "";
                }

                lenlec = GetField(html, "lenlec");
                lenomb = GetField(html, "lenomb");
                leapel = GetField(html, "leapel");
                leddni = GetField(html, "leddni");
                lemail = GetField(html, "lemail");

                var carreraMatch = Regex.Match(html,
                    @"name=""lecol2""\s+[^>]*value=""(\d+)"".*?<option\s+value=""\1""\s+selected[^>]*>([^<]+)",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                carrera = carreraMatch.Success
                    ? carreraMatch.Groups[2].Value.Trim()
                    : GetField(html, "lecol2");

                if (string.IsNullOrEmpty(carrera))
                {
                    var lecol2Val = GetField(html, "lecol2");
                    var sucursalMatch = Regex.Match(html,
                        $@"comboObj\(""lecol2"".*?\[\s*""{Regex.Escape(lecol2Val)}""\s*,\s*""([^""]+)""",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    carrera = sucursalMatch.Success ? sucursalMatch.Groups[1].Value.Trim() : lecol2Val;
                }
            }

            // Si no se pudo obtener el código del lector, consideramos que no se encontró la ficha
            if (string.IsNullOrEmpty(lenlec))
                return null;

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
