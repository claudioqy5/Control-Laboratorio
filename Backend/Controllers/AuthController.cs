using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static readonly ConcurrentDictionary<int, DateTime> ActiveSessionPings = new ConcurrentDictionary<int, DateTime>();
        
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
                // Como el equipo está en la pantalla de Login, cualquier sesión activa es huérfana (el agente se reinició o cerró mal)
                // Cerramos la sesión activa huérfana automáticamente.
                sesionActiva.HoraFin = TimeHelper.GetPeruTime();
                
                // Asegurar que no exceda el límite del día si era de ayer
                if (sesionActiva.Fecha.Date < TimeHelper.GetPeruTime().Date)
                {
                    sesionActiva.HoraFin = sesionActiva.HoraLimite ?? sesionActiva.HoraInicio.AddHours(3);
                }
                
                await _context.SaveChangesAsync();
            }

            // Verificar si EL ALUMNO ya tiene una sesión abierta en CUALQUIER otro equipo.
            // En lugar de bloquear el ingreso, cerramos la sesión anterior automáticamente.
            // El Agent del equipo anterior detectará el cierre en su próximo poll (cada 3s)
            // y ejecutará ForceLogout(), mostrando la pantalla de bloqueo sin intervención manual.
            var sesionAlumnoActiva = await _context.Sesiones
                .Include(s => s.Equipo)
                .FirstOrDefaultAsync(s => s.AlumnoID == alumno.AlumnoID && s.HoraFin == null);

            if (sesionAlumnoActiva != null)
            {
                var ahora = TimeHelper.GetPeruTime();

                // Si la sesión es de un día anterior, es huérfana: cerrar con HoraLimite para no distorsionar estadísticas.
                if (sesionAlumnoActiva.Fecha.Date < ahora.Date)
                {
                    sesionAlumnoActiva.HoraFin = sesionAlumnoActiva.HoraLimite ?? sesionAlumnoActiva.HoraInicio.AddHours(3);
                }
                else
                {
                    // Sesión activa del mismo día en otro equipo: cerrarla ahora.
                    // El Agent de ese equipo lo detectará en su próximo poll y bloqueará la pantalla.
                    sesionAlumnoActiva.HoraFin = ahora;
                }

                // Remover del diccionario de heartbeats para que el SessionMonitorService
                // no interfiera con el proceso de cierre que ya estamos haciendo aquí.
                ActiveSessionPings.TryRemove(sesionAlumnoActiva.SesionID, out _);

                await _context.SaveChangesAsync();
            }

            // --- LÓGICA DE BOLSA DE TIEMPO DIARIA ---
            var sesionesHoy = await _context.Sesiones
                .Where(s => s.AlumnoID == alumno.AlumnoID && s.Fecha.Date == TimeHelper.GetPeruTime().Date && s.HoraFin != null)
                .ToListAsync();

            // Si es su primer inicio del día, restablecer el límite diario a 3 horas (10800 seg)
            if (sesionesHoy.Count == 0 && alumno.LimiteDiarioSegundos != 10800)
            {
                alumno.LimiteDiarioSegundos = 10800;
                await _context.SaveChangesAsync();
            }

            double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin.Value - s.HoraInicio).TotalSeconds);
            double segundosLimite = alumno.LimiteDiarioSegundos;
            double segundosRestantes;

            if (segundosConsumidosHoy >= segundosLimite)
            {
                // Alumno consumió el límite. Si Estado=true, el admin lo reactivó.
                if (alumno.Estado)
                {
                    if (alumno.LimiteDiarioSegundos <= segundosConsumidosHoy)
                    {
                        alumno.LimiteDiarioSegundos = (int)segundosConsumidosHoy + 10800;
                        await _context.SaveChangesAsync();
                        segundosLimite = alumno.LimiteDiarioSegundos;
                    }
                    segundosRestantes = segundosLimite - segundosConsumidosHoy;
                    if (segundosRestantes <= 0) segundosRestantes = 10800; // Por si acaso
                }
                else
                {
                    alumno.Estado = false;
                    await _context.SaveChangesAsync();
                    return Unauthorized(new { message = $"Has consumido tu límite de {(alumno.LimiteDiarioSegundos / 3600)} horas por el día de hoy." });
                }
            }
            else
            {
                double tiempoRestante = segundosLimite - segundosConsumidosHoy;

                // CORRECCIÓN DE RACE CONDITION:
                bool tieneSessionesPreviasHoy = sesionesHoy.Count > 0;
                bool tiempoRestanteAnomalo = tiempoRestante < (15 * 60); // menos de 15 minutos

                if (tiempoRestanteAnomalo && tieneSessionesPreviasHoy && alumno.Estado)
                {
                    // Si se detecta un desfase (race condition), incrementamos el límite en 3 horas adicionales
                    alumno.LimiteDiarioSegundos = (alumno.LimiteDiarioSegundos < 10800 ? 10800 : alumno.LimiteDiarioSegundos) + 10800;
                    await _context.SaveChangesAsync();
                    segundosRestantes = alumno.LimiteDiarioSegundos - segundosConsumidosHoy;
                }
                else
                {
                    segundosRestantes = tiempoRestante > 0 ? tiempoRestante : segundosLimite;
                }
            }

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

            ActiveSessionPings[nuevaSesion.SesionID] = TimeHelper.GetPeruTime();

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
            
            ActiveSessionPings.TryRemove(request.SesionId, out _);

            // Calcular el total consumido hoy
            if (sesion.Alumno != null)
            {
                var sesionesHoy = await _context.Sesiones
                    .Where(s => s.AlumnoID == sesion.AlumnoID && s.Fecha.Date == TimeHelper.GetPeruTime().Date && s.HoraFin != null)
                    .ToListAsync();

                double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin.Value - s.HoraInicio).TotalSeconds);
                
                // Si consumió su límite diario (con un pequeño margen de 1 minuto por retrasos de red)
                if (segundosConsumidosHoy >= (sesion.Alumno.LimiteDiarioSegundos - 60))
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
            
            ActiveSessionPings[sesionId] = TimeHelper.GetPeruTime();
            
            double remainingSeconds = 0;
            if (sesion.HoraLimite.HasValue)
            {
                remainingSeconds = (sesion.HoraLimite.Value - TimeHelper.GetPeruTime()).TotalSeconds;
                if (remainingSeconds < 0) remainingSeconds = 0;
            }

            return Ok(new { horaLimite = sesion.HoraLimite, isFinished = sesion.HoraFin != null, remainingSeconds = remainingSeconds });
        }

        [HttpPost("extend-session")]
        public async Task<IActionResult> ExtendSession([FromBody] ExtendSessionRequest request)
        {
            var sesion = await _context.Sesiones.Include(s => s.Alumno).FirstOrDefaultAsync(s => s.SesionID == request.SesionId);
            if (sesion == null) return NotFound(new { message = "Sesión no encontrada." });
            if (sesion.HoraFin != null) return BadRequest(new { message = "La sesión ya ha finalizado." });
            if (sesion.Alumno == null) return BadRequest(new { message = "Alumno no encontrado." });

            // Sumar 3 horas (10800 seg) al límite diario del alumno
            sesion.Alumno.LimiteDiarioSegundos += 10800;

            // Extender la hora límite de la sesión actual en 3 horas
            sesion.HoraLimite = (sesion.HoraLimite ?? TimeHelper.GetPeruTime()).AddHours(3);

            await _context.SaveChangesAsync();

            double newRemaining = (sesion.HoraLimite.Value - TimeHelper.GetPeruTime()).TotalSeconds;
            if (newRemaining < 0) newRemaining = 0;

            return Ok(new { message = "Sesión extendida exitosamente.", remainingSeconds = newRemaining, horaLimite = sesion.HoraLimite });
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

        private static readonly ConcurrentDictionary<string, int> PendingUnlocks = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<string, DateTime> PendingShutdowns = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

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

        [HttpPost("trigger-remote-unlock-all")]
        public async Task<IActionResult> TriggerRemoteUnlockAll([FromQuery] bool onlyFree = false)
        {
            var equipos = await _context.Equipos.ToListAsync();
            var now = TimeHelper.GetPeruTime();
            var today = now.Date;

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

            foreach (var equipo in equipos)
            {
                // Cerramos sesión activa si la hubiera
                var sesionActiva = await _context.Sesiones
                    .Include(s => s.Alumno)
                    .FirstOrDefaultAsync(s => s.EquipoID == equipo.EquipoID && s.HoraFin == null);
                
                if (sesionActiva != null)
                {
                    if (onlyFree)
                    {
                        // Si onlyFree es true y hay sesión activa, ignoramos este equipo
                        continue;
                    }

                    sesionActiva.HoraFin = now;
                    // También actualizamos estado del alumno si corresponde (opcional, pero buena práctica)
                    if (sesionActiva.Alumno != null)
                    {
                        var sesionesHoy = await _context.Sesiones
                            .Where(s => s.AlumnoID == sesionActiva.AlumnoID && s.Fecha.Date == today && s.HoraFin != null)
                            .ToListAsync();
                        double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);
                        sesionActiva.Alumno.Estado = segundosConsumidosHoy < (sesionActiva.Alumno.LimiteDiarioSegundos - 60);
                    }
                }

                // Crear sesión virtual para el administrador
                var sesion = new Sesion
                {
                    AlumnoID = admin.AlumnoID,
                    EquipoID = equipo.EquipoID,
                    Fecha = today,
                    HoraInicio = now,
                    HoraLimite = now.AddHours(5)
                };
                _context.Sesiones.Add(sesion);
                await _context.SaveChangesAsync(); // Se necesita guardar para generar SesionID

                PendingUnlocks[equipo.NombreRed] = sesion.SesionID;
            }

            return Ok(new { message = "Comando de desbloqueo enviado a todos los equipos." });
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
                await _context.SaveChangesAsync();

                if (sesionActiva.Alumno != null)
                {
                    var sesionesHoy = await _context.Sesiones
                        .Where(s => s.AlumnoID == sesionActiva.AlumnoID && s.Fecha.Date == TimeHelper.GetPeruTime().Date && s.HoraFin != null)
                        .ToListAsync();

                    double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin.Value - s.HoraInicio).TotalSeconds);
                    
                    if (segundosConsumidosHoy >= (sesionActiva.Alumno.LimiteDiarioSegundos - 60))
                    {
                        sesionActiva.Alumno.Estado = false;
                    }
                    else
                    {
                        sesionActiva.Alumno.Estado = true;
                    }
                }
            }

            PendingShutdowns[nombreRed] = TimeHelper.GetPeruTime();
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Comando de apagado enviado al equipo {nombreRed}." });
        }

        [HttpPost("trigger-remote-shutdown-all")]
        public async Task<IActionResult> TriggerRemoteShutdownAll()
        {
            var equipos = await _context.Equipos.ToListAsync();
            var now = TimeHelper.GetPeruTime();
            var today = now.Date;

            foreach (var e in equipos)
            {
                // Registrar comando de apagado para TODOS los equipos (con o sin sesión activa)
                PendingShutdowns[e.NombreRed] = now;

                // Si tiene sesión activa, la cerramos
                var sesionActiva = await _context.Sesiones
                    .Include(s => s.Alumno)
                    .FirstOrDefaultAsync(s => s.EquipoID == e.EquipoID && s.HoraFin == null);

                if (sesionActiva != null)
                {
                    sesionActiva.HoraFin = now;

                    if (sesionActiva.Alumno != null)
                    {
                        var sesionesHoy = await _context.Sesiones
                            .Where(s => s.AlumnoID == sesionActiva.AlumnoID && s.Fecha.Date == today && s.HoraFin != null)
                            .ToListAsync();

                        double segundosConsumidosHoy = sesionesHoy.Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);

                        sesionActiva.Alumno.Estado = segundosConsumidosHoy < (sesionActiva.Alumno.LimiteDiarioSegundos - 60);
                    }
                }
            }

            // Un único guardado al final para evitar conflictos de concurrencia
            await _context.SaveChangesAsync();
            return Ok(new { message = "Comando de apagado enviado a todos los equipos." });
        }

        [HttpGet("check-remote-unlock/{nombreRed}")]
        public IActionResult CheckRemoteUnlock(string nombreRed)
        {
            bool shouldShutdown = false;
            if (PendingShutdowns.TryGetValue(nombreRed, out DateTime shutdownTime))
            {
                // Solo apagar si el comando fue enviado hace menos de 10 minutos (evita apagados fantasmas en encendidos del día siguiente)
                if ((TimeHelper.GetPeruTime() - shutdownTime).TotalMinutes <= 10)
                {
                    shouldShutdown = true;
                }
                PendingShutdowns.TryRemove(nombreRed, out _); // Consumir comando
            }

            if (PendingUnlocks.TryGetValue(nombreRed, out int sesionId))
            {
                PendingUnlocks.TryRemove(nombreRed, out _);
                return Ok(new { unlock = true, sesionId = sesionId, shutdown = shouldShutdown });
            }
            return Ok(new { unlock = false, shutdown = shouldShutdown });
        }

        [HttpGet("check-remote-shutdown/{nombreRed}")]
        public IActionResult CheckRemoteShutdown(string nombreRed)
        {
            bool shouldShutdown = false;
            if (PendingShutdowns.TryGetValue(nombreRed, out DateTime shutdownTime))
            {
                if ((TimeHelper.GetPeruTime() - shutdownTime).TotalMinutes <= 10)
                {
                    shouldShutdown = true;
                }
                PendingShutdowns.TryRemove(nombreRed, out _); // Consumir comando
            }
            return Ok(new { shutdown = shouldShutdown });
        }
    }
}
