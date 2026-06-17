using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Helpers;
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
    }
}
