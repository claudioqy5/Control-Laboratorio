using ControlLaboratorio.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                .Select(e => new
                {
                    e.EquipoID,
                    e.NombreRed,
                    e.Ubicacion,
                    e.Estado,
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
        public async Task<IActionResult> GetDashboardStats()
        {
            var now = DateTime.Now;
            var todayStart = now.Date;
            var lastMonth = now.AddMonths(-1);

            var sesionesActivas = await _context.Sesiones.CountAsync(s => s.HoraFin == null);
            var totalEstaciones = await _context.Equipos.CountAsync();
            var sesionesHoy = await _context.Sesiones.CountAsync(s => s.HoraInicio >= todayStart);

            var closedSessions = await _context.Sesiones
                .Where(s => s.HoraFin != null && s.HoraInicio >= lastMonth)
                .ToListAsync();

            var tiempoPromedioMinutos = closedSessions.Any() 
                ? closedSessions.Average(s => (s.HoraFin!.Value - s.HoraInicio).TotalMinutes)
                : 0;

            var afluenciaPorHoraRaw = await _context.Sesiones
                .Where(s => s.HoraInicio >= lastMonth)
                .GroupBy(s => s.HoraInicio.Hour)
                .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(k => k.Hora, v => v.Cantidad);

            var afluenciaPorHora = Enumerable.Range(7, 15).Select(h => afluenciaPorHoraRaw.ContainsKey(h) ? afluenciaPorHoraRaw[h] : 0).ToList();

            var afluenciaPorDiaRaw = await _context.Sesiones
                .Where(s => s.HoraInicio >= now.AddDays(-6))
                .GroupBy(s => s.HoraInicio.Date)
                .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(k => k.Fecha, v => v.Cantidad);

            var afluenciaPorDia = Enumerable.Range(0, 7).Select(i => {
                var d = now.Date.AddDays(-6 + i);
                return new {
                    Dia = d.ToString("dd/MM"),
                    Cantidad = afluenciaPorDiaRaw.ContainsKey(d) ? afluenciaPorDiaRaw[d] : 0
                };
            }).ToList();

            var distribucionCarrera = await _context.Sesiones
                .Where(s => s.HoraInicio >= lastMonth)
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
    }
}
