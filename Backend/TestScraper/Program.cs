using Microsoft.Playwright;
using System.Text.RegularExpressions;

class TestPlaywright
{
    static async Task Main(string[] args)
    {
        var codigo = args.Length > 0 ? args[0] : "202512445";
        Console.WriteLine($"=== Playwright scraper para código: {codigo} ===");

        // Instalar browsers si no están instalados
        Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,  // Cambiar a false para ver el browser
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();

        try
        {
            Console.WriteLine("1. Navegando al login...");
            await page.GotoAsync("https://biblioteca.urp.edu.pe/abnet/inicio.htm");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            Console.WriteLine("2. Haciendo login...");
            // El login tiene frames - buscar los campos
            await page.FillAsync("input[name='USER'], #USER, input[type='text']", "medicina");
            await page.FillAsync("input[name='PASS'], #PASS, input[type='password']", "biblioteca1");
            await page.ClickAsync("input[type='submit'], button[type='submit'], input[value*='cceder'], input[value*='ntr']");
            
            // Esperar que cargue el sistema
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Console.WriteLine($"URL después del login: {page.Url}");

            // El sistema usa frames - necesitamos acceder al frame AbxMenu primero
            await Task.Delay(2000);
            
            // Obtener todos los frames
            var frames = page.Frames;
            Console.WriteLine($"Frames disponibles: {frames.Count}");
            foreach (var f in frames)
                Console.WriteLine($"  Frame: {f.Name} - {f.Url}");

            // Buscar el frame del menú (AbxMenu)
            var menuFrame = page.Frame("AbxMenu");
            var mainFrame = page.Frame("AbxMain");
            
            if (menuFrame != null)
            {
                Console.WriteLine($"\n3. Menú frame URL: {menuFrame.Url}");
                // Buscar y hacer click en "Lectores" → "Gestión de lectores"
                var lectoresLink = await menuFrame.QuerySelectorAsync("text=Gestión de lectores, a:has-text('Gestión'), a:has-text('Gesti')");
                if (lectoresLink != null)
                {
                    Console.WriteLine("   Haciendo click en Gestión de lectores...");
                    await lectoresLink.ClickAsync();
                    await Task.Delay(2000);
                }
                else
                {
                    Console.WriteLine("   No se encontró el link. Buscando en el menú...");
                    var links = await menuFrame.QuerySelectorAllAsync("a");
                    foreach (var link in links)
                    {
                        var text = await link.TextContentAsync();
                        Console.WriteLine($"   Link: {text?.Trim()}");
                    }
                }
            }

            // Re-obtener mainFrame después de la navegación
            mainFrame = page.Frame("AbxMain");
            if (mainFrame != null)
            {
                Console.WriteLine($"\n4. Main frame URL: {mainFrame.Url}");
                
                // Buscar el campo Nº lector
                var lenlecInput = await mainFrame.QuerySelectorAsync("#lenlec, input[name='lenlec']");
                if (lenlecInput == null)
                {
                    Console.WriteLine("   Campo lenlec no encontrado. Intentando navegación directa...");
                    var matchUrl = Regex.Match(mainFrame.Url, @"(/abnet/abnetcl\.exe/X\d+/[IU]D\w+/)");
                    if (matchUrl.Success)
                    {
                        string targetUrl = $"https://biblioteca.urp.edu.pe{matchUrl.Groups[1].Value}NT1?ACC=110&TB=29";
                        Console.WriteLine($"   Navegando directamente a: {targetUrl}");
                        await mainFrame.GotoAsync(targetUrl);
                        await mainFrame.WaitForLoadStateAsync(LoadState.NetworkIdle);
                        await Task.Delay(2000);
                        lenlecInput = await mainFrame.QuerySelectorAsync("#lenlec, input[name='lenlec']");
                    }
                }

                if (lenlecInput != null)
                {
                    // Print details and close the dialog if present
                    var dialogClosed = await page.EvaluateAsync<bool>(@"() => {
                        const diag = document.querySelector('dialog');
                        if (diag) {
                            const closeBtn = document.querySelector('#dialog-close');
                            if (closeBtn) closeBtn.click();
                            diag.remove();
                            return true;
                        }
                        return false;
                    }");
                    Console.WriteLine($"   Parent Dialog closed: {dialogClosed}");

                    Console.WriteLine("   Campo lenlec encontrado. Ingresando código y presionando Enter...");
                    await lenlecInput.FocusAsync();
                    await lenlecInput.FillAsync(codigo);
                    
                    // We can also try pressing Enter to submit
                    await lenlecInput.PressAsync("Enter");
                    await Task.Delay(4000); // Wait for the submit/load to complete
                    
                    // See if the URL changed or if new content loaded
                    var currentUrl = mainFrame.Url;
                    Console.WriteLine($"   URL actual después de Enter: {currentUrl}");
                    
                    var html = await mainFrame.ContentAsync();
                    
                    // If it hasn't loaded the data, let's see if we need to navigate to ACC=112 (first reg) under the new URL
                    if (!html.Contains("DYLAN") && !currentUrl.Contains("ACC=112"))
                    {
                        var matchAfter = Regex.Match(currentUrl, @"(/abnet/abnetcl\.exe/X\d+/[IU]D\w+/)NT(\d+)");
                        if (matchAfter.Success)
                        {
                            string firstRegUrl = $"https://biblioteca.urp.edu.pe{matchAfter.Groups[1].Value}NT{matchAfter.Groups[2].Value}?ACC=112";
                            Console.WriteLine($"   Navegando al primer registro: {firstRegUrl}");
                            await mainFrame.GotoAsync(firstRegUrl);
                            await mainFrame.WaitForLoadStateAsync(LoadState.NetworkIdle);
                            await Task.Delay(2000);
                            html = await mainFrame.ContentAsync();
                        }
                    }

                    html = await mainFrame.ContentAsync();
                    Console.WriteLine($"\nHTML de búsqueda/resultado ({html.Length} chars):");
                    var inputs = await mainFrame.EvaluateAsync<string[]>(@"() => {
                        return Array.from(document.querySelectorAll('input, select, textarea'))
                            .map(el => `${el.name || el.id || 'no-name'}: value=[${el.value}], type=[${el.type}]`);
                    }");
                    Console.WriteLine("   Form fields found:");
                    foreach (var input in inputs)
                        Console.WriteLine($"      {input}");

                    if (html.Contains("DYLAN", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("*** DYLAN/CASTILLO ENCONTRADO ***");
                        
                        var doc = new HtmlAgilityPack.HtmlDocument();
                        doc.LoadHtml(html);

                        // Helper to get text after a LabelR cell
                        Func<string, string> getVal = (label) =>
                        {
                            var lblNode = doc.DocumentNode.SelectSingleNode($"//td[contains(@class, 'LabelR') and (normalize-space(text())='{label}' or contains(text(), '{label}'))]");
                            if (lblNode == null) return "";
                            
                            // Get the immediate next TD sibling
                            var nextTd = lblNode.SelectSingleNode("following-sibling::td[1]");
                            if (nextTd != null)
                            {
                                // If there are nested Inp1 cells, find the last non-empty one (useful for Tr./Inic./Nombre)
                                var nestedInpNodes = nextTd.SelectNodes(".//td[contains(@class, 'Inp1')]");
                                if (nestedInpNodes != null && nestedInpNodes.Count > 0)
                                {
                                    for (int i = nestedInpNodes.Count - 1; i >= 0; i--)
                                    {
                                        var txt = nestedInpNodes[i].InnerText.Replace("&nbsp;", " ").Trim();
                                        if (!string.IsNullOrEmpty(txt)) return txt;
                                    }
                                }

                                // Otherwise, use nextTd itself
                                var valNode = nextTd.SelectSingleNode(".//span") ?? nextTd;
                                var fontNode = valNode.SelectSingleNode(".//font");
                                if (fontNode != null) return fontNode.InnerText.Trim();
                                return valNode.InnerText.Replace("&nbsp;", " ").Trim();
                            }
                            return "";
                        };

                        var parsedCodigo = getVal("Nº lector");
                        var parsedNombre = getVal("Tr./Inic./Nombre");
                        var parsedApellidos = getVal("Apellidos");
                        var parsedDni = getVal("DNI");
                        var parsedCarrera = getVal("Sucursal");

                        Console.WriteLine($"   Parsed Datos:");
                        Console.WriteLine($"      Código: [{parsedCodigo}]");
                        Console.WriteLine($"      Nombre: [{parsedNombre}]");
                        Console.WriteLine($"      Apellidos: [{parsedApellidos}]");
                        Console.WriteLine($"      DNI: [{parsedDni}]");
                        Console.WriteLine($"      Carrera/Sucursal: [{parsedCarrera}]");
                    }
                }
                else
                {
                    Console.WriteLine("   No se encontró el campo lenlec ni con navegación directa.");
                }
            }
            else
            {
                Console.WriteLine("No se encontró AbxMain frame");
                // Listar todos los frames nuevamente
                foreach (var f in page.Frames)
                    Console.WriteLine($"  Frame: '{f.Name}' - {f.Url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            await browser.CloseAsync();
        }
    }
}
