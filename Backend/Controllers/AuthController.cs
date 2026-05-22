using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;


        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("register-equipment")]
        public async Task<IActionResult> RegisterEquipment([FromBody] RegisterEquipmentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreRed))
            {
                return BadRequest(new { message = "El nombre de red es requerido." });
            }

            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NombreRed == request.NombreRed);
            if (equipo == null)
            {
                equipo = new Equipo { NombreRed = request.NombreRed, Ubicacion = "Laboratorio Central", Estado = true };
                _context.Equipos.Add(equipo);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Equipo registrado." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var alumno = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.CodigoUniversitario == request.CodigoUniversitario && a.DNI == request.DNI);

            if (alumno == null)
            {
                return Unauthorized(new { message = "Credenciales incorrectas." });
            }

            if (!alumno.Estado)
            {
                return Unauthorized(new { message = "El alumno se encuentra inactivo." });
            }

            var equipo = await _context.Equipos
                .FirstOrDefaultAsync(e => e.NombreRed == request.NombreRed);

            if (equipo == null)
            {
                // Si el equipo no existe, lo registramos (opcional, dependiendo de la política)
                equipo = new Equipo { NombreRed = request.NombreRed, Ubicacion = "Laboratorio Central", Estado = true };
                _context.Equipos.Add(equipo);
                await _context.SaveChangesAsync();
            }

            if (!equipo.Estado)
            {
                return BadRequest(new { message = "Este equipo está fuera de servicio." });
            }

            // Verificar si ya tiene una sesión activa en este equipo (para evitar duplicados)
            var sesionActiva = await _context.Sesiones
                .FirstOrDefaultAsync(s => s.EquipoID == equipo.EquipoID && s.HoraFin == null);

            if (sesionActiva != null)
            {
                // Podríamos cerrarla o simplemente no permitir el login
                return BadRequest(new { message = "El equipo ya tiene una sesión activa." });
            }

            // --- LÓGICA DE BOLSA DE TIEMPO DIARIA ---
            var sesionesHoy = await _context.Sesiones
                .Where(s => s.AlumnoID == alumno.AlumnoID && s.Fecha.Date == TimeHelper.GetPeruTime().Date && s.HoraFin != null)
                .ToListAsync();

            double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin.Value - s.HoraInicio).TotalSeconds);
            double segundosLimite = 3 * 3600; // 3 horas

            if (segundosConsumidosHoy >= segundosLimite)
            {
                alumno.Estado = false;
                await _context.SaveChangesAsync();
                return Unauthorized(new { message = "Has consumido tu límite de 3 horas por el día de hoy." });
            }

            double segundosRestantes = segundosLimite - segundosConsumidosHoy;

            var nuevaSesion = new Sesion
            {
                AlumnoID = alumno.AlumnoID,
                EquipoID = equipo.EquipoID,
                Fecha = TimeHelper.GetPeruTime().Date,
                HoraInicio = TimeHelper.GetPeruTime(),
                HoraLimite = TimeHelper.GetPeruTime().AddSeconds(segundosRestantes)
            };

            _context.Sesiones.Add(nuevaSesion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                sesionId = nuevaSesion.SesionID,
                horaLimite = nuevaSesion.HoraLimite,
                remainingSeconds = segundosRestantes,
                alumno = new
                {
                    nombres = alumno.Nombres,
                    apellidos = $"{alumno.ApellidoPaterno} {alumno.ApellidoMaterno}"
                }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var sesion = await _context.Sesiones
                .Include(s => s.Alumno)
                .FirstOrDefaultAsync(s => s.SesionID == request.SesionId);

            if (sesion == null)
            {
                return NotFound(new { message = "Sesión no encontrada." });
            }

            sesion.HoraFin = TimeHelper.GetPeruTime();
            await _context.SaveChangesAsync();

            // Calcular el total consumido hoy
            if (sesion.Alumno != null)
            {
                var sesionesHoy = await _context.Sesiones
                    .Where(s => s.AlumnoID == sesion.AlumnoID && s.Fecha.Date == TimeHelper.GetPeruTime().Date && s.HoraFin != null)
                    .ToListAsync();

                double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin.Value - s.HoraInicio).TotalSeconds);
                
                // Si consumió sus 3 horas (con un pequeño margen de 1 minuto por retrasos de red)
                if (segundosConsumidosHoy >= (3 * 3600 - 60))
                {
                    sesion.Alumno.Estado = false;
                }
                else
                {
                    sesion.Alumno.Estado = true;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Sesión finalizada." });
        }

        [HttpPost("admin-login")]
        public IActionResult AdminLogin([FromBody] AdminLoginRequest request)
        {
            // Credenciales de administrador fijas (puedes cambiarlas aquí)
            if (request.Username == "admin" && request.Password == "admin123")
            {
                return Ok(new { token = "fake-jwt-token-for-now", message = "Bienvenido Administrador" });
            }

            return Unauthorized(new { message = "Usuario o contraseña incorrectos." });
        }

        [HttpGet("session-status/{sesionId}")]
        public async Task<IActionResult> GetSessionStatus(int sesionId)
        {
            var sesion = await _context.Sesiones.FindAsync(sesionId);
            if (sesion == null) return NotFound();
            
            double remainingSeconds = 0;
            if (sesion.HoraLimite.HasValue)
            {
                remainingSeconds = (sesion.HoraLimite.Value - TimeHelper.GetPeruTime()).TotalSeconds;
                if (remainingSeconds < 0) remainingSeconds = 0;
            }

            return Ok(new { horaLimite = sesion.HoraLimite, isFinished = sesion.HoraFin != null, remainingSeconds = remainingSeconds });
        }

        [HttpPost("set-limit")]
        public async Task<IActionResult> SetSessionLimit([FromBody] SetSessionLimitRequest request)
        {
            var sesion = await _context.Sesiones.FindAsync(request.SesionId);
            if (sesion == null) return NotFound(new { message = "Sesión no encontrada." });
            if (sesion.HoraFin != null) return BadRequest(new { message = "La sesión ya ha finalizado." });

            sesion.HoraLimite = request.NuevaHoraLimite;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tiempo actualizado.", horaLimite = sesion.HoraLimite });
        }

        [HttpGet("active-session/{nombreRed}")]
        public async Task<IActionResult> GetActiveSession(string nombreRed)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NombreRed == nombreRed);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado." });

            var sesionActiva = await _context.Sesiones
                .Include(s => s.Alumno)
                .FirstOrDefaultAsync(s => s.EquipoID == equipo.EquipoID && s.HoraFin == null);

            if (sesionActiva == null) return Ok(new { hasActiveSession = false });

            double remainingSeconds = 0;
            if (sesionActiva.HoraLimite.HasValue)
            {
                remainingSeconds = (sesionActiva.HoraLimite.Value - TimeHelper.GetPeruTime()).TotalSeconds;
                if (remainingSeconds < 0) remainingSeconds = 0;
            }

            return Ok(new
            {
                hasActiveSession = true,
                sesionId = sesionActiva.SesionID,
                horaLimite = sesionActiva.HoraLimite,
                remainingSeconds = remainingSeconds,
                alumno = new
                {
                    nombres = sesionActiva.Alumno?.Nombres,
                    apellidos = $"{sesionActiva.Alumno?.ApellidoPaterno} {sesionActiva.Alumno?.ApellidoMaterno}"
                }
            });
        }

        private static readonly Dictionary<string, int> PendingUnlocks = new Dictionary<string, int>();
        private static readonly HashSet<string> PendingShutdowns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        [HttpPost("trigger-remote-unlock/{nombreRed}")]
        public async Task<IActionResult> TriggerRemoteUnlock(string nombreRed)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NombreRed == nombreRed);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado." });

            // Buscar o crear el alumno Administrador para el registro
            var admin = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == "ADMIN");
            if (admin == null)
            {
                admin = new Alumno 
                { 
                    CodigoUniversitario = "ADMIN", 
                    DNI = "00000000", 
                    Nombres = "ADMINISTRADOR", 
                    ApellidoPaterno = "SISTEMA",
                    ApellidoMaterno = "BVE",
                    CorreoInstitucional = "admin@bve.com",
                    Carrera = "SOPORTE"
                };
                _context.Alumnos.Add(admin);
                await _context.SaveChangesAsync();
            }

            // Crear una sesión virtual para el administrador
            var sesion = new Sesion
            {
                AlumnoID = admin.AlumnoID,
                EquipoID = equipo.EquipoID,
                Fecha = TimeHelper.GetPeruTime().Date,
                HoraInicio = TimeHelper.GetPeruTime(),
                HoraLimite = TimeHelper.GetPeruTime().AddHours(5) // 5 horas por defecto para admin
            };
            _context.Sesiones.Add(sesion);
            await _context.SaveChangesAsync();

            PendingUnlocks[nombreRed] = sesion.SesionID;
            return Ok(new { message = "Desbloqueo enviado al equipo.", sesionId = sesion.SesionID });
        }

        [HttpPost("trigger-remote-shutdown/{nombreRed}")]
        public async Task<IActionResult> TriggerRemoteShutdown(string nombreRed)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.NombreRed == nombreRed);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado." });

            // Si tiene una sesión activa, la cerramos automáticamente
            var sesionActiva = await _context.Sesiones
                .Include(s => s.Alumno)
                .FirstOrDefaultAsync(s => s.EquipoID == equipo.EquipoID && s.HoraFin == null);
            if (sesionActiva != null)
            {
                sesionActiva.HoraFin = TimeHelper.GetPeruTime();
                if (sesionActiva.Alumno != null)
                {
                    sesionActiva.Alumno.Estado = false;
                }
            }

            PendingShutdowns.Add(nombreRed);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Comando de apagado enviado al equipo {nombreRed}." });
        }

        [HttpPost("trigger-remote-shutdown-all")]
        public async Task<IActionResult> TriggerRemoteShutdownAll()
        {
            var equipos = await _context.Equipos.ToListAsync();
            foreach (var e in equipos)
            {
                // Cerrar sesiones activas de todos
                var sesionActiva = await _context.Sesiones
                    .Include(s => s.Alumno)
                    .FirstOrDefaultAsync(s => s.EquipoID == e.EquipoID && s.HoraFin == null);
                if (sesionActiva != null)
                {
                    sesionActiva.HoraFin = TimeHelper.GetPeruTime();
                    if (sesionActiva.Alumno != null)
                    {
                        sesionActiva.Alumno.Estado = false;
                    }
                }
                PendingShutdowns.Add(e.NombreRed);
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Comando de apagado enviado a todos los equipos." });
        }

        [HttpGet("check-remote-unlock/{nombreRed}")]
        public IActionResult CheckRemoteUnlock(string nombreRed)
        {
            bool shouldShutdown = false;
            if (PendingShutdowns.Contains(nombreRed))
            {
                shouldShutdown = true;
                PendingShutdowns.Remove(nombreRed); // Consumir comando
            }

            if (PendingUnlocks.ContainsKey(nombreRed))
            {
                int sesionId = PendingUnlocks[nombreRed];
                PendingUnlocks.Remove(nombreRed);
                return Ok(new { unlock = true, sesionId = sesionId, shutdown = shouldShutdown });
            }
            return Ok(new { unlock = false, shutdown = shouldShutdown });
        }
    }
}
