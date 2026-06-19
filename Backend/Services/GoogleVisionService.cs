using Google.Cloud.Vision.V1;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.Services
{
    public class GoogleVisionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GoogleVisionService> _logger;
        private readonly ApplicationDbContext _context;

        public GoogleVisionService(IWebHostEnvironment env, ILogger<GoogleVisionService> logger, ApplicationDbContext context)
        {
            _env = env;
            _logger = logger;
            _context = context;
        }

        public async Task<ParsedStudentData?> ScanCarnetAsync(string base64Image)
        {
            // 1. Validar límite mensual antes de llamar a la API
            var primerDiaMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            int escaneosEsteMes = await _context.ScanLogs.CountAsync(s => s.Fecha >= primerDiaMes);

            if (escaneosEsteMes >= 950)
            {
                _logger.LogWarning($"Se ha alcanzado el límite de seguridad mensual de escaneos (actual: {escaneosEsteMes}). Solicitud bloqueada.");
                throw new InvalidOperationException("Se ha superado el límite mensual de escaneos permitidos en el sistema.");
            }

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

                // 2. Registrar el escaneo exitoso en la base de datos
                _context.ScanLogs.Add(new ScanLog());
                await _context.SaveChangesAsync();

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
            List<string> cleanLines = lines.Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

            var result = new ParsedStudentData();

            // Regex patterns
            var codigoRegex = new Regex(@"\b(20\d{6,8})\b");
            var dniRegex = new Regex(@"\b(\d{8})\b");

            // Words that are definitely NOT names/careers - used to filter noise
            var noiseWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "expira", "expiración", "expiracion", "emision", "emisión", "valido", "validez",
                "republica", "peru", "sunedu", "ministerio", "superintendencia", "nacional",
                "educacion", "educación", "superior", "universitaria", "universitario",
                "carnet", "carné", "firma", "codigo", "código", "libre"
            };

            // ─── Step 1: Extract Código and DNI ───────────────────────────────
            foreach (var line in cleanLines)
            {
                if (string.IsNullOrEmpty(result.CodigoUniversitario))
                {
                    var m = codigoRegex.Match(line);
                    if (m.Success) result.CodigoUniversitario = m.Groups[1].Value;
                }
                if (string.IsNullOrEmpty(result.DNI))
                {
                    var m = dniRegex.Match(line);
                    if (m.Success)
                    {
                        string val = m.Groups[1].Value;
                        if (!val.StartsWith("20") || val == result.CodigoUniversitario)
                            result.DNI = val;
                    }
                }
            }

            // Fallback DNI: any 8-digit number that isn't the code
            if (string.IsNullOrEmpty(result.DNI))
            {
                foreach (var line in cleanLines)
                {
                    foreach (Match m in dniRegex.Matches(line))
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

            // ─── Step 2: Find keyword line indices ────────────────────────────
            int apellidosIndex = -1, nombresIndex = -1, carreraIndex = -1, facultadIndex = -1;

            for (int i = 0; i < cleanLines.Count; i++)
            {
                string lower = cleanLines[i].ToLower();
                if (lower.Contains("apellido")) apellidosIndex = i;
                else if (lower.Contains("nombre")) nombresIndex = i;
                else if (lower.Contains("carrera") || lower.Contains("escuela") || lower.Contains("especialidad") || lower.Contains("programa")) carreraIndex = i;
                else if (lower.Contains("facultad")) facultadIndex = i;
            }

            // ─── Step 3: Extract Apellidos (may span multiple lines) ──────────
            if (apellidosIndex != -1)
            {
                // Collect lines after the "Apellidos:" label until we hit another keyword or a noise line
                var apellidoParts = new List<string>();
                int start = apellidosIndex;
                string firstVal = GetValueFromLine(cleanLines, start);

                // If the label itself has a value on the same line (e.g. "Apellidos: GARCIA")
                if (!string.IsNullOrWhiteSpace(firstVal) && !IsNoiseOrKeyword(firstVal, noiseWords))
                    apellidoParts.Add(firstVal.ToUpper());

                // Look ahead for continuation lines (next lines that are all-caps names)
                int nextKeyword = new[] { nombresIndex, carreraIndex, facultadIndex }
                    .Where(x => x > apellidosIndex)
                    .DefaultIfEmpty(cleanLines.Count)
                    .Min();

                // If the value was on the next line (GetValueFromLine returned lines[apellidosIndex+1])
                int lookStart = (string.IsNullOrWhiteSpace(firstVal)) ? apellidosIndex + 1 : apellidosIndex + 2;

                for (int i = lookStart; i < nextKeyword && i < cleanLines.Count; i++)
                {
                    string candidate = cleanLines[i].Trim();
                    if (IsNoiseOrKeyword(candidate, noiseWords)) break;
                    if (codigoRegex.IsMatch(candidate) || dniRegex.IsMatch(candidate)) break;
                    if (candidate.Any(char.IsDigit)) break;
                    if (candidate.Length < 2) break;
                    // Only all-uppercase words (typical of printed carnets)
                    if (candidate == candidate.ToUpper() || candidate.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || "ÁÉÍÓÚÑ".Contains(char.ToUpper(c))))
                        apellidoParts.Add(candidate.ToUpper());
                    else
                        break;
                }

                if (apellidoParts.Count > 0)
                {
                    result.Apellidos = string.Join(" ", apellidoParts);

                    if (apellidoParts.Count == 1)
                    {
                        // Both surnames on same line: "CASTILLO DIAZ" → split by first word
                        var words = apellidoParts[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length >= 2)
                        {
                            result.ApellidoPaterno = words[0];
                            result.ApellidoMaterno = string.Join(" ", words.Skip(1));
                        }
                        else
                        {
                            result.ApellidoPaterno = apellidoParts[0];
                            result.ApellidoMaterno = "";
                        }
                    }
                    else
                    {
                        // Each surname on its own line: apellidoParts[0] = paterno, rest = materno
                        // e.g. ["CASTILLO", "DIAZ"] or ["CUEVA", "DEL AGUILA"]
                        result.ApellidoPaterno = apellidoParts[0];
                        result.ApellidoMaterno = string.Join(" ", apellidoParts.Skip(1));
                    }
                }
            }

            // ─── Step 4: Extract Nombres ──────────────────────────────────────
            if (nombresIndex != -1)
            {
                string val = GetValueFromLine(cleanLines, nombresIndex);
                if (!string.IsNullOrWhiteSpace(val) && !IsNoiseOrKeyword(val, noiseWords))
                    result.Nombres = val.ToUpper();
                else
                {
                    // Try next line
                    int nextIdx = nombresIndex + 1;
                    // Skip the line that GetValueFromLine already consumed
                    if (!val.Contains(":") && nextIdx < cleanLines.Count)
                        nextIdx = nombresIndex + 2;

                    for (int i = nombresIndex + 1; i < cleanLines.Count && i < nombresIndex + 4; i++)
                    {
                        string candidate = cleanLines[i].Trim();
                        if (IsNoiseOrKeyword(candidate, noiseWords)) continue;
                        if (codigoRegex.IsMatch(candidate) || dniRegex.IsMatch(candidate)) continue;
                        if (candidate.Any(char.IsDigit)) continue;
                        if (candidate.Length > 1 && !candidate.ToLower().Contains("apellido") && !candidate.ToLower().Contains("carrera") && !candidate.ToLower().Contains("facultad"))
                        {
                            result.Nombres = candidate.ToUpper();
                            break;
                        }
                    }
                }
            }

            // ─── Step 5: Extract Carrera ──────────────────────────────────────
            // Prefer "Carrera:" over "Facultad:"
            int carreraLookIndex = carreraIndex != -1 ? carreraIndex : facultadIndex;
            if (carreraLookIndex != -1)
            {
                string val = GetValueFromLine(cleanLines, carreraLookIndex);
                if (!string.IsNullOrWhiteSpace(val) && !IsNoiseOrKeyword(val, noiseWords))
                    result.Carrera = val;
                else if (carreraLookIndex + 1 < cleanLines.Count)
                {
                    for (int i = carreraLookIndex + 1; i < cleanLines.Count && i < carreraLookIndex + 3; i++)
                    {
                        string candidate = cleanLines[i].Trim();
                        if (!IsNoiseOrKeyword(candidate, noiseWords) && !codigoRegex.IsMatch(candidate) && !dniRegex.IsMatch(candidate) && candidate.Length > 2)
                        {
                            result.Carrera = candidate;
                            break;
                        }
                    }
                }
            }

            // ─── Step 6: Split apellidos if still not split ───────────────────
            if (!string.IsNullOrEmpty(result.Apellidos) && string.IsNullOrEmpty(result.ApellidoPaterno))
            {
                string cleaned = CleanLabel(result.Apellidos, "apellidos");
                var parts = cleaned.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    result.ApellidoPaterno = parts[0];
                    result.ApellidoMaterno = string.Join(" ", parts.Skip(1));
                }
                else if (parts.Length == 1)
                {
                    result.ApellidoPaterno = parts[0];
                }
            }

            // Clean labels from all fields
            if (!string.IsNullOrEmpty(result.Nombres)) result.Nombres = CleanLabel(result.Nombres, "nombres");
            if (!string.IsNullOrEmpty(result.Carrera))
            {
                result.Carrera = CleanLabel(result.Carrera, "carrera");
                result.Carrera = CleanLabel(result.Carrera, "escuela");
                result.Carrera = CleanLabel(result.Carrera, "facultad");
                result.Carrera = CleanLabel(result.Carrera, "programa");
            }

            return result;
        }

        private bool IsNoiseOrKeyword(string text, HashSet<string> noiseWords)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            string lower = text.ToLower().Trim();
            // Check if the text is or starts with a noise word
            if (noiseWords.Any(nw => lower == nw || lower.StartsWith(nw))) return true;
            // Check if it looks like a date (contains digits and dashes/slashes)
            if (Regex.IsMatch(lower, @"\d{2,4}[-/]\d{1,2}")) return true;
            return false;
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
