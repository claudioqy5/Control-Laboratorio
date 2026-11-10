using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.API.DTOs;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ConferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static ParticipacionConferencia CrearDesdeAlumno(
            int idConferencia,
            Alumno alumno,
            byte estado,
            DateTime? fechaAsistencia)
        {
            var ahora = DateTime.UtcNow;
            return new ParticipacionConferencia
            {
                IdConferencia = idConferencia,
                TipoParticipante = "Estudiante",
                TipoDocumento = "DNI",
                NumeroDocumento = alumno.DNI,
                CodigoURP = alumno.CodigoUniversitario,
                Nombres = alumno.Nombres,
                ApellidoPaterno = alumno.ApellidoPaterno,
                ApellidoMaterno = alumno.ApellidoMaterno,
                Correo = $"{alumno.CodigoUniversitario}@urp.edu.pe",
                FechaHoraInscripcion = ahora,
                FechaHoraAsistencia = fechaAsistencia,
                EstadoParticipacion = estado,
                AlumnoID = alumno.AlumnoID
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetConferences()
        {
            var conferencias = await _context.ConferenciasMedicas
                .Where(c => c.EstadoEvento != "Cancelado")
                .OrderBy(c => c.FechaHoraInicio)
                .Select(c => new
                {
                    c.IdConferencia,
                    c.TituloEvento,
                    c.ExpositorPonente,
                    c.EntidadEditorial,
                    c.FechaHoraInicio,
                    c.FechaHoraFin,
                    c.Modalidad,
                    c.Ubicacion_o_Enlace,
                    c.AsistenciaAbierta,
                    c.EstadoEvento,
                    TotalInscritos = c.Participaciones.Count(p => p.FechaHoraInscripcion != p.FechaHoraAsistencia),
                    TotalAsistentes = c.Participaciones.Count(p => p.EstadoParticipacion == 2)
                })
                .ToListAsync();

            return Ok(conferencias);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetConference(int id)
        {
            var conferencia = await _context.ConferenciasMedicas.FindAsync(id);
            if (conferencia == null)
            {
                return NotFound();
            }

            return Ok(conferencia);
        }

        [HttpGet("check-student-status")]
        public async Task<IActionResult> CheckStudentStatus([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest();
            }

            var docLimpio = code.Trim();
            var existe = await _context.Alumnos.AnyAsync(a => a.CodigoUniversitario == docLimpio || a.DNI == docLimpio);

            return Ok(new { status = existe ? "PRESENTE" : "AUSENTE" });
        }

        [HttpPost("{id}/register")]
        public async Task<IActionResult> Register(int id, [FromBody] PreInscripcionDto dto)
        {
            var conferencia = await _context.ConferenciasMedicas.FindAsync(id);
            if (conferencia == null)
            {
                return NotFound(new { message = "Conferencia no encontrada." });
            }

            var cod = string.IsNullOrWhiteSpace(dto.CodigoURP) ? null : dto.CodigoURP.Trim();
            var doc = dto.NumeroDocumento?.Trim() ?? string.Empty;

            var yaExiste = await _context.ParticipacionesConferencia.AnyAsync(p =>
                p.IdConferencia == id && ((cod != null && p.CodigoURP == cod) || p.NumeroDocumento == doc));

            if (yaExiste)
            {
                return Conflict(new { message = "El participante ya se encuentra registrado en esta conferencia." });
            }

            ParticipacionConferencia participacion;

            if (dto.TipoParticipante == "Estudiante")
            {
                var alumno = await _context.Alumnos.FirstOrDefaultAsync(a =>
                    (cod != null && a.CodigoUniversitario == cod) || a.DNI == doc);

                if (alumno != null)
                {
                    participacion = CrearDesdeAlumno(id, alumno, estado: 1, fechaAsistencia: null);
                }
                else
                {
                    participacion = new ParticipacionConferencia
                    {
                        IdConferencia = id,
                        TipoParticipante = "Estudiante",
                        TipoDocumento = dto.TipoDocumento?.Trim() ?? "DNI",
                        NumeroDocumento = doc,
                        CodigoURP = cod,
                        Nombres = dto.Nombres?.Trim() ?? string.Empty,
                        ApellidoPaterno = dto.ApellidoPaterno?.Trim() ?? string.Empty,
                        ApellidoMaterno = dto.ApellidoMaterno?.Trim(),
                        Correo = dto.Correo?.Trim() ?? string.Empty,
                        FechaHoraInscripcion = DateTime.UtcNow,
                        FechaHoraAsistencia = null,
                        EstadoParticipacion = 1,
                        AlumnoID = null
                    };
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.ApellidoPaterno))
                {
                    return BadRequest(new { message = "El apellido paterno es obligatorio." });
                }

                participacion = new ParticipacionConferencia
                {
                    IdConferencia = id,
                    TipoParticipante = dto.TipoParticipante?.Trim() ?? "Externo",
                    TipoDocumento = dto.TipoDocumento?.Trim() ?? "DNI",
                    NumeroDocumento = doc,
                    CodigoURP = cod,
                    Nombres = dto.Nombres?.Trim() ?? string.Empty,
                    ApellidoPaterno = dto.ApellidoPaterno.Trim(),
                    ApellidoMaterno = dto.ApellidoMaterno?.Trim(),
                    Correo = dto.Correo?.Trim() ?? string.Empty,
                    FechaHoraInscripcion = DateTime.UtcNow,
                    FechaHoraAsistencia = null,
                    EstadoParticipacion = 1,
                    AlumnoID = null
                };
            }

            _context.ParticipacionesConferencia.Add(participacion);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Pre-inscripción confirmada.", id = participacion.IdParticipacion });
        }

        [HttpPost("{id}/attendances/mark")]
        public async Task<IActionResult> MarkAttendance(int id, [FromBody] MarkAttendanceDto dto)
        {
            var conferencia = await _context.ConferenciasMedicas.FindAsync(id);
            if (conferencia == null || !conferencia.AsistenciaAbierta)
            {
                return BadRequest(new { message = "La conferencia no existe o la toma de asistencia no está abierta." });
            }

            var doc = dto.DocumentoOCodigo?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(doc))
            {
                return BadRequest(new { message = "El documento o código es requerido." });
            }

            var participacion = await _context.ParticipacionesConferencia.FirstOrDefaultAsync(p =>
                p.IdConferencia == id && (p.CodigoURP == doc || p.NumeroDocumento == doc));

            if (participacion != null)
            {
                if (participacion.EstadoParticipacion == 2)
                {
                    return Ok(new
                    {
                        status = "YA_REGISTRADO",
                        mensaje = "El participante ya registró su asistencia previamente.",
                        nombreCompleto = $"{participacion.Nombres} {participacion.ApellidoPaterno} {participacion.ApellidoMaterno}".Trim(),
                        hora = participacion.FechaHoraAsistencia
                    });
                }

                participacion.EstadoParticipacion = 2;
                participacion.FechaHoraAsistencia = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    status = "ACREDITADO",
                    mensaje = "Asistencia confirmada con éxito (Pre-inscrito).",
                    nombreCompleto = $"{participacion.Nombres} {participacion.ApellidoPaterno} {participacion.ApellidoMaterno}".Trim(),
                    hora = participacion.FechaHoraAsistencia
                });
            }

            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == doc || a.DNI == doc);
            if (alumno != null)
            {
                var ahora = DateTime.UtcNow;
                var espontaneo = CrearDesdeAlumno(id, alumno, estado: 2, fechaAsistencia: ahora);
                _context.ParticipacionesConferencia.Add(espontaneo);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    status = "ESPONTANEO_ALUMNO",
                    mensaje = "Asistencia espontánea confirmada (Padrón URP).",
                    nombreCompleto = $"{espontaneo.Nombres} {espontaneo.ApellidoPaterno} {espontaneo.ApellidoMaterno}".Trim(),
                    hora = ahora
                });
            }

            return NotFound(new
            {
                status = "NO_ENCONTRADO",
                mensaje = "El documento o código no se encuentra pre-inscrito ni en el padrón de alumnos."
            });
        }
    }
}
