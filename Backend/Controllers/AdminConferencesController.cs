using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.DTOs;
using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/v1/admin/conferences")]
    [ApiController]
    public class AdminConferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminConferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var conferencias = await _context.ConferenciasMedicas
                .OrderByDescending(c => c.FechaHoraInicio)
                .Select(c => new
                {
                    c.IdConferencia,
                    c.TituloEvento,
                    c.ExpositorPonente,
                    c.Modalidad,
                    c.Ubicacion_o_Enlace,
                    c.FechaHoraInicio,
                    c.FechaHoraFin,
                    c.AsistenciaAbierta,
                    c.EstadoEvento,
                    c.AutoPurgar30Dias,
                    TotalPreInscritos = c.Participaciones.Count(p => p.FechaHoraInscripcion != p.FechaHoraAsistencia),
                    TotalAsistentes = c.Participaciones.Count(p => p.EstadoParticipacion == 2)
                })
                .ToListAsync();

            return Ok(conferencias);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ConferenciaMedica conf)
        {
            conf.FechaCreacion = DateTime.UtcNow;
            if (conf.AutoPurgar30Dias)
            {
                conf.FechaCaducidadPurge = conf.FechaHoraFin.AddDays(30);
            }

            _context.ConferenciasMedicas.Add(conf);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = conf.IdConferencia }, conf);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ConferenciaMedica conf)
        {
            if (id != conf.IdConferencia)
            {
                return BadRequest();
            }

            if (conf.AutoPurgar30Dias)
            {
                conf.FechaCaducidadPurge = conf.FechaHoraFin.AddDays(30);
            }

            _context.Entry(conf).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var conf = await _context.ConferenciasMedicas.FindAsync(id);
            if (conf == null)
            {
                return NotFound();
            }

            _context.ConferenciasMedicas.Remove(conf);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/toggle-attendance")]
        public async Task<IActionResult> ToggleAttendance(int id)
        {
            var conf = await _context.ConferenciasMedicas.FindAsync(id);
            if (conf == null)
            {
                return NotFound();
            }

            conf.AsistenciaAbierta = !conf.AsistenciaAbierta;
            await _context.SaveChangesAsync();

            return Ok(new { id = conf.IdConferencia, asistenciaAbierta = conf.AsistenciaAbierta });
        }

        [HttpGet("{id}/report")]
        public async Task<IActionResult> GetReport(int id)
        {
            var participaciones = await _context.ParticipacionesConferencia
                .Where(p => p.IdConferencia == id)
                .OrderBy(p => p.ApellidoPaterno)
                .ToListAsync();

            var acreditados = participaciones.Count(p => p.EstadoParticipacion == 2 && p.FechaHoraInscripcion != p.FechaHoraAsistencia);
            var espontaneos = participaciones.Count(p => p.EstadoParticipacion == 2 && p.FechaHoraInscripcion == p.FechaHoraAsistencia);
            var inasistencias = participaciones.Count(p => p.EstadoParticipacion == 1 && p.FechaHoraAsistencia == null);
            var totalPreInscritos = acreditados + inasistencias;
            var totalAsistentesReales = acreditados + espontaneos;
            var tasaAsistencia = totalPreInscritos > 0 ? Math.Round((double)acreditados / totalPreInscritos * 100, 1) : 0.0;

            return Ok(new
            {
                kpis = new
                {
                    totalPreInscritos,
                    acreditados,
                    inasistencias,
                    espontaneos,
                    totalAsistentesReales,
                    tasaAsistencia
                },
                participantes = participaciones
            });
        }

        [HttpPut("{id}/participants/{idParticipacion}")]
        public async Task<IActionResult> QuickEditParticipant(int id, int idParticipacion, [FromBody] QuickEditParticipanteDto dto)
        {
            var part = await _context.ParticipacionesConferencia
                .FirstOrDefaultAsync(p => p.IdConferencia == id && p.IdParticipacion == idParticipacion);

            if (part == null)
            {
                return NotFound();
            }

            part.Nombres = dto.Nombres.Trim();
            part.ApellidoPaterno = dto.ApellidoPaterno.Trim();
            part.ApellidoMaterno = dto.ApellidoMaterno?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Correo))
            {
                part.Correo = dto.Correo.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(part);
        }

        [HttpPost("participants/{idParticipacion}/link-to-student")]
        public async Task<IActionResult> LinkToStudent(int idParticipacion)
        {
            var part = await _context.ParticipacionesConferencia.FindAsync(idParticipacion);
            if (part == null)
            {
                return NotFound();
            }

            if (part.AlumnoID != null)
            {
                return BadRequest("El participante ya está vinculado a un alumno.");
            }

            var nuevoAlumno = new Alumno
            {
                CodigoUniversitario = part.CodigoURP ?? part.NumeroDocumento,
                DNI = part.NumeroDocumento,
                Nombres = part.Nombres,
                ApellidoPaterno = part.ApellidoPaterno,
                ApellidoMaterno = part.ApellidoMaterno ?? string.Empty,
                CorreoInstitucional = part.Correo,
                Estado = true,
                LimiteDiarioSegundos = 3 * 3600
            };

            _context.Alumnos.Add(nuevoAlumno);
            await _context.SaveChangesAsync();

            part.AlumnoID = nuevoAlumno.AlumnoID;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Alumno dado de alta exitosamente en Cómputo.", alumnoId = nuevoAlumno.AlumnoID });
        }
    }
}
