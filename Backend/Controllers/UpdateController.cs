using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        // PCs que tienen actualización pendiente (nombreRed → versión destino)
        // Sigue el mismo patrón que PendingShutdowns en AuthController
        private static readonly ConcurrentDictionary<string, string> PendingUpdates =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public UpdateController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/update/info
        // Devuelve la versión del servidor disponible para el panel Admin
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("info")]
        public IActionResult GetServerInfo()
        {
            string version = _configuration["AgentUpdate:CurrentVersion"] ?? "1.0.0";
            string fecha = _configuration["AgentUpdate:VersionFecha"] ?? "2026-05-28";

            // Verificar si el archivo del agente existe en wwwroot/updates/
            string exePath = Path.Combine(_env.WebRootPath ?? "wwwroot", "updates", "ControlLaboratorio.Agent.exe");
            bool archivoDisponible = System.IO.File.Exists(exePath);

            return Ok(new
            {
                version,
                fecha,
                archivoDisponible
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/update/status
        // Lista todas las PCs con su versión instalada y estado
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("status")]
        public async Task<IActionResult> GetAllStatus()
        {
            string serverVersion = _configuration["AgentUpdate:CurrentVersion"] ?? "1.0.0";

            var equiposDb = await _context.Equipos
                .Select(e => new
                {
                    e.NombreRed,
                    e.AgentVersion,
                    e.AgentVersionFecha
                })
                .ToListAsync();

            var result = equiposDb.Select(e => 
            {
                bool isPendiente = PendingUpdates.ContainsKey(e.NombreRed);
                return new
                {
                    e.NombreRed,
                    versionInstalada = e.AgentVersion ?? "Desconocida",
                    fechaActualizacion = e.AgentVersionFecha,
                    estado = isPendiente
                        ? "pendiente"
                        : (e.AgentVersion == null
                            ? "sinSenal"
                            : (e.AgentVersion == serverVersion ? "alDia" : "disponible")),
                    serverVersion
                };
            });

            return Ok(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/update/report-version
        // El Agente llama esto al iniciar para reportar qué versión tiene instalada
        // Body: { nombreRed: "PC-01", version: "1.0.0" }
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("report-version")]
        public async Task<IActionResult> ReportVersion([FromBody] ReportVersionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreRed) || string.IsNullOrWhiteSpace(request.Version))
                return BadRequest(new { message = "NombreRed y Version son requeridos." });

            var equipo = await _context.Equipos
                .FirstOrDefaultAsync(e => e.NombreRed == request.NombreRed);

            if (equipo == null)
                return NotFound(new { message = "Equipo no encontrado." });

            equipo.AgentVersion = request.Version;
            equipo.AgentVersionFecha = TimeHelper.GetPeruTime();

            // Si ya se actualizó a la versión del servidor, limpiar pendiente
            string serverVersion = _configuration["AgentUpdate:CurrentVersion"] ?? "1.0.0";
            if (request.Version == serverVersion)
            {
                PendingUpdates.TryRemove(request.NombreRed, out _);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Versión registrada." });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/update/push/{nombreRed}
        // Admin ordena actualizar una PC específica
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("push/{nombreRed}")]
        public async Task<IActionResult> PushUpdate(string nombreRed)
        {
            var equipo = await _context.Equipos
                .FirstOrDefaultAsync(e => e.NombreRed == nombreRed);

            if (equipo == null)
                return NotFound(new { message = "Equipo no encontrado." });

            // Verificar que el archivo de actualización existe antes de marcar como pendiente
            string exePath = Path.Combine(_env.WebRootPath ?? "wwwroot", "updates", "ControlLaboratorio.Agent.exe");
            if (!System.IO.File.Exists(exePath))
                return BadRequest(new { message = "No hay archivo de actualización disponible en el servidor. Coloca el nuevo Agent.exe en wwwroot/updates/" });

            string serverVersion = _configuration["AgentUpdate:CurrentVersion"] ?? "1.0.0";

            // Si ya está al día, no hacer nada
            if (equipo.AgentVersion == serverVersion)
                return Ok(new { message = "El equipo ya tiene la versión más reciente.", yaEstaAlDia = true });

            PendingUpdates[nombreRed] = serverVersion;
            return Ok(new { message = $"Actualización pendiente enviada a {nombreRed}.", version = serverVersion });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/update/push-all
        // Admin ordena actualizar TODAS las PCs de golpe
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("push-all")]
        public async Task<IActionResult> PushAll()
        {
            string exePath = Path.Combine(_env.WebRootPath ?? "wwwroot", "updates", "ControlLaboratorio.Agent.exe");
            if (!System.IO.File.Exists(exePath))
                return BadRequest(new { message = "No hay archivo de actualización disponible en el servidor. Coloca el nuevo Agent.exe en wwwroot/updates/" });

            string serverVersion = _configuration["AgentUpdate:CurrentVersion"] ?? "1.0.0";

            var equipos = await _context.Equipos.ToListAsync();
            int marcadas = 0;
            int yaAlDia = 0;

            foreach (var equipo in equipos)
            {
                if (equipo.AgentVersion == serverVersion)
                {
                    yaAlDia++;
                    continue;
                }
                PendingUpdates[equipo.NombreRed] = serverVersion;
                marcadas++;
            }

            return Ok(new
            {
                message = $"Actualización enviada a {marcadas} equipo(s). {yaAlDia} ya estaban al día.",
                marcadas,
                yaAlDia
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/update/check/{nombreRed}
        // El Agente consulta periódicamente si tiene actualización pendiente
        // Regresa: { hayActualizacion: true/false, version: "X.X.X" }
        // IMPORTANTE: NO consume el pendiente aquí, solo informa.
        //             Se consume cuando el Agente reporta la nueva versión.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("check/{nombreRed}")]
        public IActionResult CheckPending(string nombreRed)
        {
            if (PendingUpdates.TryGetValue(nombreRed, out string? targetVersion))
            {
                return Ok(new { hayActualizacion = true, version = targetVersion });
            }
            return Ok(new { hayActualizacion = false });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/update/download
        // El Agente descarga el nuevo exe desde aquí
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("download")]
        public IActionResult DownloadAgent()
        {
            string exePath = Path.Combine(_env.WebRootPath ?? "wwwroot", "updates", "ControlLaboratorio.Agent.exe");

            if (!System.IO.File.Exists(exePath))
                return NotFound(new { message = "El archivo de actualización no está disponible en el servidor." });

            var fileStream = new FileStream(exePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(fileStream, "application/octet-stream", "ControlLaboratorio.Agent.exe");
        }
    }

    public class ReportVersionRequest
    {
        public string NombreRed { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}
