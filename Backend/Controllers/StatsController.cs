using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsageStats()
        {
            // Agrupar por hora del día para ver afluencia
            var stats = await _context.Sesiones
                .GroupBy(s => s.HoraInicio.Hour)
                .Select(g => new
                {
                    Hora = g.Key,
                    Cantidad = g.Count()
                })
                .OrderBy(x => x.Hora)
                .ToListAsync();

            return Ok(stats);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var active = await _context.Sesiones
                .Include(s => s.Alumno)
                .Include(s => s.Equipo)
                .Where(s => s.HoraFin == null)
                .Select(s => new
                {
                    s.SesionID,
                    Alumno = $"{s.Alumno!.Nombres} {s.Alumno.ApellidoPaterno}",
                    Equipo = s.Equipo!.NombreRed,
                    Alias = s.Equipo.Alias,
                    s.HoraInicio,
                    LimiteDiarioSegundos = s.Alumno.LimiteDiarioSegundos
                })
                .ToListAsync();

            return Ok(active);
        }

        [HttpGet("map")]
        public async Task<IActionResult> GetLaboratoryMap()
        {
            var equipos = await _context.Equipos
                .OrderBy(e => e.NombreRed)
                .Select(e => new
                {
                    e.EquipoID,
                    e.NombreRed,
                    e.Alias,
                    e.Comentario,
                    e.Ubicacion,
                    e.Estado,
                    e.PosicionMapa,
                    SesionActiva = _context.Sesiones
                        .Where(s => s.EquipoID == e.EquipoID && s.HoraFin == null)
                        .Select(s => new
                        {
                            s.SesionID,
                            Alumno = s.Alumno!.Nombres + " " + s.Alumno.ApellidoPaterno,
                            s.HoraInicio,
                            s.HoraLimite
                        }).FirstOrDefault()
                }).ToListAsync();

            return Ok(equipos);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] DateTime? date)
        {
            var targetDate = date?.Date ?? TimeHelper.GetPeruTime().Date;
            var targetDayEnd = targetDate.AddDays(1);

            // Sesiones activas (Solo tiene sentido para hoy)
            var sesionesActivas = (targetDate == TimeHelper.GetPeruTime().Date) 
                ? await _context.Sesiones.CountAsync(s => s.HoraFin == null)
                : 0;

            var totalEstaciones = await _context.Equipos.CountAsync();
            
            // Sesiones en el día seleccionado
            var sesionesHoy = await _context.Sesiones.CountAsync(s => s.HoraInicio >= targetDate && s.HoraInicio < targetDayEnd);

            var closedSessions = await _context.Sesiones
                .Where(s => s.HoraFin != null && s.HoraInicio >= targetDate && s.HoraInicio < targetDayEnd)
                .ToListAsync();

            var tiempoPromedioMinutos = closedSessions.Any() 
                ? closedSessions.Average(s => (s.HoraFin!.Value - s.HoraInicio).TotalMinutes)
                : 0;

            // Obtener todas las sesiones del día seleccionado con sus relaciones
            var sesionesDelDia = await _context.Sesiones
                .Include(s => s.Alumno)
                .Include(s => s.Equipo)
                .Where(s => s.HoraInicio >= targetDate && s.HoraInicio < targetDayEnd)
                .OrderBy(s => s.HoraInicio)
                .ToListAsync();

            // Afluencia detallada por hora del día seleccionado
            var afluenciaPorHora = Enumerable.Range(7, 15).Select(h => {
                var sesionesDeLaHora = sesionesDelDia.Where(s => s.HoraInicio.Hour == h).ToList();
                return new {
                    Hora = h,
                    Cantidad = sesionesDeLaHora.Count,
                    Sesiones = sesionesDeLaHora.Select(s => new {
                        s.SesionID,
                        CodigoUniversitario = s.Alumno?.CodigoUniversitario ?? string.Empty,
                        DNI = s.Alumno?.DNI ?? string.Empty,
                        AlumnoNombre = s.Alumno != null ? $"{s.Alumno.Nombres} {s.Alumno.ApellidoPaterno} {s.Alumno.ApellidoMaterno}".Trim() : string.Empty,
                        Nombres = s.Alumno?.Nombres ?? string.Empty,
                        ApellidoPaterno = s.Alumno?.ApellidoPaterno ?? string.Empty,
                        ApellidoMaterno = s.Alumno?.ApellidoMaterno ?? string.Empty,
                        Carrera = s.Alumno?.Carrera ?? string.Empty,
                        Telefono = s.Alumno?.Telefono ?? string.Empty,
                        CorreoInstitucional = s.Alumno?.CorreoInstitucional ?? string.Empty,
                        CorreoPersonal = s.Alumno?.CorreoPersonal ?? string.Empty,
                        Equipo = s.Equipo?.NombreRed ?? string.Empty,
                        EquipoUbicacion = s.Equipo?.Ubicacion ?? string.Empty,
                        HoraInicio = s.HoraInicio.ToString("hh:mm tt", CultureInfo.InvariantCulture),
                        HoraFin = s.HoraFin?.ToString("hh:mm tt", CultureInfo.InvariantCulture) ?? "Activo",
                        DuracionMinutos = s.HoraFin.HasValue ? (int?)(s.HoraFin.Value - s.HoraInicio).TotalMinutes : null
                    }).ToList()
                };
            }).ToList();

            // Asistencia semanal relativa al día seleccionado (los 7 días anteriores)
            var weekStart = targetDate.AddDays(-6);
            var sesionesDeLaSemana = await _context.Sesiones
                .Include(s => s.Alumno)
                .Include(s => s.Equipo)
                .Where(s => s.HoraInicio >= weekStart && s.HoraInicio < targetDayEnd)
                .OrderBy(s => s.HoraInicio)
                .ToListAsync();

            var culture = new CultureInfo("es-PE");
            var afluenciaPorDia = Enumerable.Range(0, 7).Select(i => {
                var d = weekStart.AddDays(i);
                var sesionesDelDia = sesionesDeLaSemana.Where(s => s.HoraInicio.Date == d).ToList();
                return new {
                    Dia = culture.TextInfo.ToTitleCase(d.ToString("ddd dd", culture)).Replace(".", ""),
                    FechaCompleta = d.ToString("yyyy-MM-dd"),
                    Cantidad = sesionesDelDia.Count,
                    Sesiones = sesionesDelDia.Select(s => new {
                        s.SesionID,
                        CodigoUniversitario = s.Alumno?.CodigoUniversitario ?? string.Empty,
                        DNI = s.Alumno?.DNI ?? string.Empty,
                        AlumnoNombre = s.Alumno != null ? $"{s.Alumno.Nombres} {s.Alumno.ApellidoPaterno} {s.Alumno.ApellidoMaterno}".Trim() : string.Empty,
                        Nombres = s.Alumno?.Nombres ?? string.Empty,
                        ApellidoPaterno = s.Alumno?.ApellidoPaterno ?? string.Empty,
                        ApellidoMaterno = s.Alumno?.ApellidoMaterno ?? string.Empty,
                        Carrera = s.Alumno?.Carrera ?? string.Empty,
                        Telefono = s.Alumno?.Telefono ?? string.Empty,
                        CorreoInstitucional = s.Alumno?.CorreoInstitucional ?? string.Empty,
                        CorreoPersonal = s.Alumno?.CorreoPersonal ?? string.Empty,
                        Equipo = s.Equipo?.NombreRed ?? string.Empty,
                        EquipoUbicacion = s.Equipo?.Ubicacion ?? string.Empty,
                        HoraInicio = s.HoraInicio.ToString("hh:mm tt", CultureInfo.InvariantCulture),
                        HoraFin = s.HoraFin?.ToString("hh:mm tt", CultureInfo.InvariantCulture) ?? "Activo",
                        DuracionMinutos = s.HoraFin.HasValue ? (int?)(s.HoraFin.Value - s.HoraInicio).TotalMinutes : null
                    }).ToList()
                };
            }).ToList();

            var distribucionCarrera = await _context.Sesiones
                .Where(s => s.HoraInicio >= targetDate && s.HoraInicio < targetDayEnd)
                .Include(s => s.Alumno)
                .Where(s => s.Alumno != null && !string.IsNullOrEmpty(s.Alumno.Carrera))
                .GroupBy(s => s.Alumno!.Carrera)
                .Select(g => new { Carrera = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            // Obtener sesiones para los tops (últimos 30 días)
            var baseTopDate = targetDate.AddDays(-30);
            var sesionesTops = await _context.Sesiones
                .Include(s => s.Alumno)
                .Include(s => s.Equipo)
                .Where(s => s.HoraInicio >= baseTopDate && s.HoraInicio < targetDayEnd)
                .ToListAsync();

            var peruTimeNow = TimeHelper.GetPeruTime();

            var topAlumnos = sesionesTops
                .Where(s => s.Alumno != null)
                .GroupBy(s => s.AlumnoID)
                .Select(g => new
                {
                    alumnoID = g.Key,
                    codigo = g.First().Alumno!.CodigoUniversitario,
                    nombreCompleto = $"{g.First().Alumno!.Nombres} {g.First().Alumno.ApellidoPaterno}".Trim(),
                    carrera = g.First().Alumno.Carrera,
                    totalSesiones = g.Count(),
                    totalMinutos = Math.Round(g.Sum(s => s.HoraFin.HasValue 
                        ? (s.HoraFin.Value - s.HoraInicio).TotalMinutes 
                        : (peruTimeNow - s.HoraInicio).TotalMinutes))
                })
                .OrderByDescending(x => x.totalMinutos)
                .Take(5)
                .ToList();

            var topEquipos = sesionesTops
                .Where(s => s.Equipo != null)
                .GroupBy(s => s.EquipoID)
                .Select(g => new
                {
                    equipoID = g.Key,
                    nombreRed = g.First().Equipo!.NombreRed,
                    alias = g.First().Equipo.Alias,
                    totalSesiones = g.Count(),
                    totalMinutos = Math.Round(g.Sum(s => s.HoraFin.HasValue 
                        ? (s.HoraFin.Value - s.HoraInicio).TotalMinutes 
                        : (peruTimeNow - s.HoraInicio).TotalMinutes))
                })
                .OrderByDescending(x => x.totalMinutos)
                .Take(5)
                .ToList();

            return Ok(new {
                sesionesActivas,
                totalEstaciones,
                sesionesHoy,
                tiempoPromedioMinutos = Math.Round(tiempoPromedioMinutos),
                afluenciaPorHora,
                afluenciaPorDia,
                distribucionCarrera,
                topAlumnos,
                topEquipos
            });
        }

        [HttpPost("assign-map-slot")]
        public async Task<IActionResult> AssignMapSlot([FromBody] AssignMapSlotRequest request)
        {
            if (request.Password != "admin12345")
            {
                return Unauthorized(new { message = "Contraseña de administrador incorrecta." });
            }

            var equipo = await _context.Equipos.FindAsync(request.EquipoID);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado" });

            // Remove this slot from any other PC to prevent duplicates
            if (request.PosicionMapa != null)
            {
                var existing = await _context.Equipos.FirstOrDefaultAsync(e => e.PosicionMapa == request.PosicionMapa);
                if (existing != null && existing.EquipoID != equipo.EquipoID)
                {
                    existing.PosicionMapa = null;
                }
            }

            equipo.PosicionMapa = request.PosicionMapa;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Posición asignada correctamente" });
        }
        [HttpPost("set-alias")]
        public async Task<IActionResult> SetAlias([FromBody] SetAliasRequest request)
        {
            if (request.Password != "admin12345")
            {
                return Unauthorized(new { message = "Contraseña de administrador incorrecta." });
            }

            var equipo = await _context.Equipos.FindAsync(request.EquipoID);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado" });

            equipo.Alias = string.IsNullOrWhiteSpace(request.Alias) ? null : request.Alias;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Alias actualizado correctamente" });
        }
        [HttpPost("set-comentario")]
        public async Task<IActionResult> SetComentario([FromBody] SetComentarioRequest request)
        {
            if (request.Password != "admin12345")
            {
                return Unauthorized(new { message = "Contraseña de administrador incorrecta." });
            }

            var equipo = await _context.Equipos.FindAsync(request.EquipoID);
            if (equipo == null) return NotFound(new { message = "Equipo no encontrado" });

            equipo.Comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Comentario actualizado correctamente" });
        }
    }

    public class AssignMapSlotRequest
    {
        public int EquipoID { get; set; }
        public int? PosicionMapa { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class SetAliasRequest
    {
        public int EquipoID { get; set; }
        public string? Alias { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public class SetComentarioRequest
    {
        public int EquipoID { get; set; }
        public string? Comentario { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
