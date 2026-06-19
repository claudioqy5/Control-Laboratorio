using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Helpers;
using ControlLaboratorio.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("conexiones")]
        public async Task<IActionResult> GetConexiones([FromQuery] string? fecha)
        {
            DateTime fechaConsulta;
            if (string.IsNullOrEmpty(fecha) || !DateTime.TryParse(fecha, out fechaConsulta))
            {
                fechaConsulta = TimeHelper.GetPeruTime().Date;
            }
            else
            {
                fechaConsulta = fechaConsulta.Date;
            }

            var sesiones = await _context.Sesiones
                .Include(s => s.Alumno)
                .Include(s => s.Equipo)
                .Where(s => s.Fecha.Date == fechaConsulta)
                .OrderByDescending(s => s.HoraInicio)
                .Select(s => new
                {
                    sesionId = s.SesionID,
                    alumnoNombres = s.Alumno != null ? $"{s.Alumno.Nombres} {s.Alumno.ApellidoPaterno} {s.Alumno.ApellidoMaterno}" : "Desconocido",
                    alumnoCodigo = s.Alumno != null ? s.Alumno.CodigoUniversitario : "N/A",
                    equipoRed = s.Equipo != null ? s.Equipo.NombreRed : "Desconocido",
                    equipoAlias = s.Equipo != null ? s.Equipo.Alias : null,
                    horaInicio = s.HoraInicio,
                    horaFin = s.HoraFin,
                    estado = s.HoraFin == null ? "En línea" : "Finalizado",
                    limiteDiarioSegundos = s.Alumno != null ? s.Alumno.LimiteDiarioSegundos : 10800,
                    duracionMinutos = s.HoraFin != null 
                        ? Math.Round((s.HoraFin.Value - s.HoraInicio).TotalMinutes, 1)
                        : Math.Round((TimeHelper.GetPeruTime() - s.HoraInicio).TotalMinutes, 1)
                })
                .ToListAsync();

            return Ok(new {
                fechaConsulta = fechaConsulta,
                totalConexiones = sesiones.Count,
                sesiones = sesiones
            });
        }

        [HttpGet("escaneos-stats")]
        public async Task<IActionResult> GetEscaneosStats()
        {
            var peruTime = TimeHelper.GetPeruTime();
            var primerDiaMes = new DateTime(peruTime.Year, peruTime.Month, 1);
            int escaneosEsteMes = await _context.ScanLogs.CountAsync(s => s.Fecha >= primerDiaMes);
            return Ok(new
            {
                escaneosEsteMes = escaneosEsteMes,
                limiteMensual = 1000,
                limiteSeguridad = 950
            });
        }

        [HttpGet("escaneos")]
        public async Task<IActionResult> GetEscaneos([FromQuery] string? fecha)
        {
            IQueryable<ScanLog> query = _context.ScanLogs;
            bool verTodos = string.Equals(fecha, "all", StringComparison.OrdinalIgnoreCase);

            if (!verTodos)
            {
                DateTime fechaConsulta;
                if (string.IsNullOrEmpty(fecha) || !DateTime.TryParse(fecha, out fechaConsulta))
                {
                    fechaConsulta = TimeHelper.GetPeruTime().Date;
                }
                else
                {
                    fechaConsulta = fechaConsulta.Date;
                }
                var targetDayEnd = fechaConsulta.AddDays(1);
                query = query.Where(s => s.Fecha >= fechaConsulta && s.Fecha < targetDayEnd);
            }

            var escaneos = await query
                .OrderByDescending(s => s.Fecha)
                .Select(s => new
                {
                    scanLogId = s.ScanLogID,
                    fecha = s.Fecha,
                    realizadoPor = s.RealizadoPor ?? "Lector de Carné",
                    isExitoso = s.IsExitoso,
                    mensaje = s.Mensaje,
                    alumnoCodigo = s.AlumnoCodigo,
                    alumnoNombre = s.AlumnoNombre
                })
                .ToListAsync();

            return Ok(new
            {
                fechaConsulta = verTodos ? "Todos" : (object)(string.IsNullOrEmpty(fecha) ? DateTime.Today : DateTime.Parse(fecha)),
                totalEscaneos = escaneos.Count,
                escaneos = escaneos
            });
        }
    }
}
