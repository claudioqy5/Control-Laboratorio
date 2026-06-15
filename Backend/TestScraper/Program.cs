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
                if (lenlecInput != null)
                {
                    Console.WriteLine("   Campo lenlec encontrado. Ingresando código...");
                    await lenlecInput.ClickAsync();
                    await lenlecInput.FillAsync(codigo);
                    await lenlecInput.PressAsync("Tab");
                    await Task.Delay(2000);
                    
                    // Obtener el HTML del mainFrame después de ingresar el código
                    var html = await mainFrame.ContentAsync();
                    Console.WriteLine($"\nHTML del frame principal ({html.Length} chars):");
                    
                    // Buscar campos con datos
                    foreach (Match m in Regex.Matches(html, @"name=""(lenlec|lenomb|leapel|leddni|lecol2)""\s+[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase))
                        Console.WriteLine($"  {m.Groups[1].Value} = [{m.Groups[2].Value}]");
                    
                    if (html.Contains("CASTILLO", StringComparison.OrdinalIgnoreCase))
                        Console.WriteLine("*** CASTILLO ENCONTRADO ***");
                }
                else
                {
                    Console.WriteLine("   No se encontró el campo lenlec");
                    var mainHtml = await mainFrame.ContentAsync();
                    Console.WriteLine($"   HTML del main ({mainHtml.Length} chars): {mainHtml[..Math.Min(500, mainHtml.Length)]}");
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
