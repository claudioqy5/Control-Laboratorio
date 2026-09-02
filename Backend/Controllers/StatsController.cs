using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using ControlLaboratorio.API.Models;

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
        public async Task<IActionResult> GetDashboardStats([FromQuery] DateTime? date, [FromQuery] int? month, [FromQuery] int? year, [FromQuery] string? mode = null)
        {
            var targetDate = date?.Date ?? TimeHelper.GetPeruTime().Date;
            if (mode == "year" && year.HasValue)
            {
                var peruNow = TimeHelper.GetPeruTime();
                targetDate = peruNow.Year == year.Value ? peruNow.Date : new DateTime(year.Value, 1, 1);
            }
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

            // Determinar rango de fechas para las estadísticas principales (sesionesTops)
            DateTime baseTopDate;
            DateTime endTopDate;

            if (mode == "date" || (date.HasValue && !month.HasValue && !year.HasValue))
            {
                // Modo Día Exacto: se analizan únicamente las sesiones de ese día exacto
                baseTopDate = targetDate;
                endTopDate = targetDayEnd;
            }
            else if (mode == "year" || (year.HasValue && !month.HasValue))
            {
                // Modo Año Completo: todo el año seleccionado
                var targetYear = year ?? targetDate.Year;
                baseTopDate = new DateTime(targetYear, 1, 1);
                endTopDate = new DateTime(targetYear + 1, 1, 1);
            }
            else if (month.HasValue && year.HasValue)
            {
                // Modo Mes: mes y año específicos
                baseTopDate = new DateTime(year.Value, month.Value, 1);
                endTopDate = baseTopDate.AddMonths(1);
            }
            else
            {
                // Fallback: últimos 30 días
                baseTopDate = targetDate.AddDays(-30);
                endTopDate = targetDayEnd;
            }

            var sesionesTops = await _context.Sesiones
                .Include(s => s.Alumno)
                .Include(s => s.Equipo)
                .Where(s => s.HoraInicio >= baseTopDate && s.HoraInicio < endTopDate)
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

            // Rendimiento total de todas las computadoras para el mapa
            var todosLosEquipos = await _context.Equipos.ToListAsync();
            var usoPorEquipo = sesionesTops
                .GroupBy(s => s.EquipoID)
                .ToDictionary(
                    g => g.Key,
                    g => new {
                        TotalSesiones = g.Count(),
                        TotalMinutos = Math.Round(g.Sum(s => s.HoraFin.HasValue 
                            ? (s.HoraFin.Value - s.HoraInicio).TotalMinutes 
                            : (peruTimeNow - s.HoraInicio).TotalMinutes))
                    }
                );

            var equiposRendimiento = todosLosEquipos.Select(e => {
                var stats = usoPorEquipo.ContainsKey(e.EquipoID) ? usoPorEquipo[e.EquipoID] : null;
                return new {
                    e.EquipoID,
                    e.NombreRed,
                    e.Alias,
                    e.Comentario,
                    e.Ubicacion,
                    e.PosicionMapa,
                    totalSesiones = stats?.TotalSesiones ?? 0,
                    totalMinutos = stats?.TotalMinutos ?? 0
                };
            }).ToList();

            // Reporte completo de alumnos para descargar Excel
            var reporteAlumnos = sesionesTops
                .Where(s => s.Alumno != null)
                .GroupBy(s => s.AlumnoID)
                .Select(g => new
                {
                    alumnoID = g.Key,
                    codigo = g.First().Alumno!.CodigoUniversitario,
                    dni = g.First().Alumno.DNI,
                    nombreCompleto = $"{g.First().Alumno!.Nombres} {g.First().Alumno.ApellidoPaterno} {g.First().Alumno.ApellidoMaterno}".Trim(),
                    carrera = g.First().Alumno.Carrera,
                    correoInstitucional = g.First().Alumno.CorreoInstitucional,
                    totalSesiones = g.Count(),
                    totalMinutos = Math.Round(g.Sum(s => s.HoraFin.HasValue 
                        ? (s.HoraFin.Value - s.HoraInicio).TotalMinutes 
                        : (peruTimeNow - s.HoraInicio).TotalMinutes))
                })
                .OrderByDescending(x => x.totalMinutos)
                .ToList();

            var distribucionCarrera30Dias = sesionesTops
                .Where(s => s.Alumno != null && !string.IsNullOrEmpty(s.Alumno.Carrera))
                .GroupBy(s => s.Alumno!.Carrera)
                .Select(g => new { Carrera = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
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
                topEquipos,
                equiposRendimiento,
                reporteAlumnos,
                distribucionCarrera30Dias
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

        [HttpPost("seed-test-data")]
        public async Task<IActionResult> SeedTestData([FromBody] SeedTestDataRequest request)
        {
            if (request.Password != "admin12345")
            {
                return Unauthorized(new { message = "Contraseña de administrador incorrecta." });
            }

            // Crear alumnos de prueba si no hay
            if (!await _context.Alumnos.AnyAsync())
            {
                var alumnos = new List<Alumno>
                {
                    new Alumno { CodigoUniversitario = "20201001", DNI = "11111111", Nombres = "Juan", ApellidoPaterno = "Perez", ApellidoMaterno = "Gomez", Carrera = "Medicina Humana" },
                    new Alumno { CodigoUniversitario = "20201002", DNI = "22222222", Nombres = "Maria", ApellidoPaterno = "Lopez", ApellidoMaterno = "Diaz", Carrera = "Biologia" },
                    new Alumno { CodigoUniversitario = "20201003", DNI = "33333333", Nombres = "Carlos", ApellidoPaterno = "Ruiz", ApellidoMaterno = "Vega", Carrera = "Medicina Humana" },
                    new Alumno { CodigoUniversitario = "20201004", DNI = "44444444", Nombres = "Ana", ApellidoPaterno = "Torres", ApellidoMaterno = "Soto", Carrera = "Enfermeria" },
                    new Alumno { CodigoUniversitario = "20201005", DNI = "55555555", Nombres = "Luis", ApellidoPaterno = "Rojas", ApellidoMaterno = "Luna", Carrera = "Nutricion" }
                };
                _context.Alumnos.AddRange(alumnos);
                await _context.SaveChangesAsync();
            }

            // Crear equipos si no hay
            if (!await _context.Equipos.AnyAsync())
            {
                var equipos = new List<Equipo>
                {
                    new Equipo { NombreRed = "DESKTOP-01", Alias = "PC-01", PosicionMapa = 1 },
                    new Equipo { NombreRed = "DESKTOP-02", Alias = "PC-02", PosicionMapa = 2 },
                    new Equipo { NombreRed = "DESKTOP-03", Alias = "PC-03", PosicionMapa = 3 },
                    new Equipo { NombreRed = "DESKTOP-04", Alias = "PC-04", PosicionMapa = 4 },
                    new Equipo { NombreRed = "DESKTOP-05", Alias = "PC-05", PosicionMapa = 5 }
                };
                _context.Equipos.AddRange(equipos);
                await _context.SaveChangesAsync();
            }

            var alumnosList = await _context.Alumnos.ToListAsync();
            var equiposList = await _context.Equipos.ToListAsync();
            var random = new Random();

            var sesiones = new List<Sesion>();
            
            // Generar datos para los ultimos 6 meses
            for (int monthOffset = 0; monthOffset < 6; monthOffset++)
            {
                var targetMonth = DateTime.Now.AddMonths(-monthOffset);
                int daysInMonth = DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month);
                
                // 35 sesiones por mes
                for (int i = 0; i < 35; i++)
                {
                    var day = random.Next(1, daysInMonth + 1);
                    var hour = random.Next(8, 20); // 8 AM to 8 PM
                    var minute = random.Next(0, 60);
                    
                    var startTime = new DateTime(targetMonth.Year, targetMonth.Month, day, hour, minute, 0);
                    var durationMinutes = random.Next(15, 180); // 15 mins to 3 hours
                    var endTime = startTime.AddMinutes(durationMinutes);

                    var alumno = alumnosList[random.Next(alumnosList.Count)];
                    var equipo = equiposList[random.Next(equiposList.Count)];

                    sesiones.Add(new Sesion
                    {
                        AlumnoID = alumno.AlumnoID,
                        EquipoID = equipo.EquipoID,
                        Fecha = startTime.Date,
                        HoraInicio = startTime,
                        HoraFin = endTime
                    });
                }
            }

            _context.Sesiones.AddRange(sesiones);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Datos de prueba generados exitosamente. (aprox 210 sesiones creadas)" });
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
    
    public class SeedTestDataRequest
    {
        public string Password { get; set; } = string.Empty;
    }
}
