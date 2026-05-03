using ControlLaboratorio.API.Data;
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
                    s.HoraInicio
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
            var targetDate = date?.Date ?? DateTime.Now.Date;
            var targetDayEnd = targetDate.AddDays(1);

            // Sesiones activas (Solo tiene sentido para hoy)
            var sesionesActivas = (targetDate == DateTime.Now.Date) 
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

            // Afluencia por hora del día seleccionado
            var afluenciaPorHoraRaw = await _context.Sesiones
                .Where(s => s.HoraInicio >= targetDate && s.HoraInicio < targetDayEnd)
                .GroupBy(s => s.HoraInicio.Hour)
                .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(k => k.Hora, v => v.Cantidad);

            var afluenciaPorHora = Enumerable.Range(7, 15).Select(h => afluenciaPorHoraRaw.ContainsKey(h) ? afluenciaPorHoraRaw[h] : 0).ToList();

            // Asistencia semanal relativa al día seleccionado (los 7 días anteriores)
            var weekStart = targetDate.AddDays(-6);
            var afluenciaPorDiaRaw = await _context.Sesiones
                .Where(s => s.HoraInicio >= weekStart && s.HoraInicio < targetDayEnd)
                .GroupBy(s => s.HoraInicio.Date)
                .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(k => k.Fecha, v => v.Cantidad);

            var culture = new CultureInfo("es-PE");
            var afluenciaPorDia = Enumerable.Range(0, 7).Select(i => {
                var d = weekStart.AddDays(i);
                return new {
                    Dia = culture.TextInfo.ToTitleCase(d.ToString("ddd dd", culture)).Replace(".", ""),
                    Cantidad = afluenciaPorDiaRaw.ContainsKey(d) ? afluenciaPorDiaRaw[d] : 0
                };
            }).ToList();

            var distribucionCarrera = await _context.Sesiones
                .Where(s => s.HoraInicio >= targetDate && s.HoraInicio < targetDayEnd)
                .Include(s => s.Alumno)
                .Where(s => s.Alumno != null && !string.IsNullOrEmpty(s.Alumno.Carrera))
                .GroupBy(s => s.Alumno!.Carrera)
                .Select(g => new { Carrera = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            return Ok(new {
                sesionesActivas,
                totalEstaciones,
                sesionesHoy,
                tiempoPromedioMinutos = Math.Round(tiempoPromedioMinutos),
                afluenciaPorHora,
                afluenciaPorDia,
                distribucionCarrera
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
    }

    public class AssignMapSlotRequest
    {
        public int EquipoID { get; set; }
        public int? PosicionMapa { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
