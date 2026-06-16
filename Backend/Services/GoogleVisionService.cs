using Google.Cloud.Vision.V1;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ControlLaboratorio.API.Services
{
    public class GoogleVisionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GoogleVisionService> _logger;

        public GoogleVisionService(IWebHostEnvironment env, ILogger<GoogleVisionService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<ParsedStudentData?> ScanCarnetAsync(string base64Image)
        {
            try
            {
                // Set the credentials environment variable dynamically
                string credentialsPath = Path.Combine(_env.ContentRootPath, "google-credentials.json");
                if (!File.Exists(credentialsPath))
                {
                    _logger.LogError($"No se encontró el archivo de credenciales en: {credentialsPath}");
                    throw new FileNotFoundException("Archivo de credenciales no encontrado.");
                }

                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);

                // Convert base64 to byte array
                if (base64Image.Contains(","))
                {
                    base64Image = base64Image.Split(',')[1];
                }
                byte[] imageBytes = Convert.FromBase64String(base64Image);
                var image = Image.FromBytes(imageBytes);

                // Call Vision API
                var client = await ImageAnnotatorClient.CreateAsync();
                var response = await client.DetectDocumentTextAsync(image);

                if (response == null || string.IsNullOrEmpty(response.Text))
                {
                    _logger.LogWarning("No se detectó ningún texto en la imagen.");
                    return null;
                }

                _logger.LogInformation($"Texto detectado de Vision API:\n{response.Text}");

                return ParseStudentData(response.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al escanear carnet con Google Vision API");
                throw;
            }
        }

        private ParsedStudentData ParseStudentData(string text)
        {
            string normalizedText = text.Replace("\r", "").Trim();
            string[] lines = normalizedText.Split('\n');

            var result = new ParsedStudentData();

            // Regex patterns
            // Código Universitario: usually starts with 20... followed by digits (length 8-10)
            var codigoRegex = new Regex(@"\b(20\d{6,8})\b");
            // DNI: exactly 8 digits
            var dniRegex = new Regex(@"\b(\d{8})\b");

            foreach (var line in lines)
            {
                var cleanLine = line.Trim();
                
                // Try to match DNI
                if (string.IsNullOrEmpty(result.DNI))
                {
                    var matchDni = dniRegex.Match(cleanLine);
                    if (matchDni.Success)
                    {
                        string val = matchDni.Groups[1].Value;
                        // Sunedu student code usually starts with 20, but DNI does not typically start with 20 in recent student IDs
                        if (val.Length == 8 && !val.StartsWith("20"))
                        {
                            result.DNI = val;
                        }
                        else if (string.IsNullOrEmpty(result.CodigoUniversitario) && val.StartsWith("20"))
                        {
                            result.CodigoUniversitario = val;
                        }
                    }
                }

                // Try to match Code
                if (string.IsNullOrEmpty(result.CodigoUniversitario))
                {
                    var matchCodigo = codigoRegex.Match(cleanLine);
                    if (matchCodigo.Success)
                    {
                        result.CodigoUniversitario = matchCodigo.Groups[1].Value;
                    }
                }
            }

            // Fallback for DNI: find any other 8-digit number that isn't the code
            if (string.IsNullOrEmpty(result.DNI))
            {
                foreach (var line in lines)
                {
                    var matches = dniRegex.Matches(line);
                    foreach (Match m in matches)
                    {
                        if (m.Value != result.CodigoUniversitario)
                        {
                            result.DNI = m.Value;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(result.DNI)) break;
                }
            }

            List<string> cleanLines = lines.Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

            // Heuristics for Names, Surnames and Career
            int apellidosIndex = -1;
            int nombresIndex = -1;
            int carreraIndex = -1;

            for (int i = 0; i < cleanLines.Count; i++)
            {
                string lower = cleanLines[i].ToLower();
                if (lower.Contains("apellido")) apellidosIndex = i;
                if (lower.Contains("nombre")) nombresIndex = i;
                if (lower.Contains("carrera") || lower.Contains("escuela") || lower.Contains("especialidad") || lower.Contains("programa")) carreraIndex = i;
            }

            if (apellidosIndex != -1) result.Apellidos = GetValueFromLine(cleanLines, apellidosIndex);
            if (nombresIndex != -1) result.Nombres = GetValueFromLine(cleanLines, nombresIndex);
            if (carreraIndex != -1) result.Carrera = GetValueFromLine(cleanLines, carreraIndex);

            // Fallback heuristics if fields are empty
            if (string.IsNullOrEmpty(result.Apellidos) || string.IsNullOrEmpty(result.Nombres))
            {
                var ignoredKeywords = new List<string> { 
                    "republica", "peru", "universidad", "sunedu", "ministerio", "carnet", "nacional", 
                    "educacion", "valido", "caducidad", "emision", "firma", "codigo", "estudiante", "libre"
                };

                var potentialNameLines = cleanLines.Where(line => 
                    !ignoredKeywords.Any(kw => line.ToLower().Contains(kw)) &&
                    !codigoRegex.IsMatch(line) &&
                    !dniRegex.IsMatch(line) &&
                    line.Length > 2 &&
                    line.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c == ',' || c == 'ñ' || c == 'Ñ' || c == 'á' || c == 'é' || c == 'í' || c == 'ó' || c == 'ú' || c == 'Á' || c == 'É' || c == 'Í' || c == 'Ó' || c == 'Ú')
                ).ToList();

                if (potentialNameLines.Count >= 2)
                {
                    if (string.IsNullOrEmpty(result.Apellidos)) result.Apellidos = potentialNameLines[0];
                    if (string.IsNullOrEmpty(result.Nombres)) result.Nombres = potentialNameLines[1];
                    if (string.IsNullOrEmpty(result.Carrera) && potentialNameLines.Count >= 3) result.Carrera = potentialNameLines[2];
                }
            }

            // Split Apellidos into Paterno and Materno
            if (!string.IsNullOrEmpty(result.Apellidos))
            {
                // Remove prefixing labels or clean up
                string cleanApellidos = CleanLabel(result.Apellidos, "apellidos");
                var parts = cleanApellidos.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    result.ApellidoPaterno = parts[0];
                    result.ApellidoMaterno = parts[1];
                }
                else if (parts.Length == 1)
                {
                    result.ApellidoPaterno = parts[0];
                }
            }

            if (!string.IsNullOrEmpty(result.Nombres))
            {
                result.Nombres = CleanLabel(result.Nombres, "nombres");
            }

            if (!string.IsNullOrEmpty(result.Carrera))
            {
                result.Carrera = CleanLabel(result.Carrera, "carrera");
                result.Carrera = CleanLabel(result.Carrera, "escuela");
                result.Carrera = CleanLabel(result.Carrera, "programa");
            }

            return result;
        }

        private string CleanLabel(string value, string label)
        {
            string clean = value;
            if (clean.ToLower().StartsWith(label.ToLower()))
            {
                clean = clean.Substring(label.Length).Trim();
                if (clean.StartsWith(":") || clean.StartsWith("-"))
                {
                    clean = clean.Substring(1).Trim();
                }
            }
            return clean;
        }

        private string GetValueFromLine(List<string> lines, int index)
        {
            string line = lines[index];
            if (line.Contains(":"))
            {
                var parts = line.Split(':');
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    return parts[1].Trim();
                }
            }

            if (index + 1 < lines.Count)
            {
                return lines[index + 1].Trim();
            }

            return "";
        }
    }

    public class ParsedStudentData
    {
        public string CodigoUniversitario { get; set; } = string.Empty;
        public string DNI { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
    }
}
