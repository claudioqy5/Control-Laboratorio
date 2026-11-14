using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.DTOs;
using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/admin/cursos")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminCursosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminCursosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? ciclo, [FromQuery] string? search)
        {
            var query = _context.CursosMedicina
                .Include(c => c.RecursosCursos)
                .ThenInclude(rc => rc.BaseDatos)
                .AsNoTracking()
                .AsQueryable();

            if (ciclo.HasValue)
            {
                query = query.Where(c => c.Ciclo == ciclo.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c => c.NombreCurso.Contains(term) || (c.CodigoCurso != null && c.CodigoCurso.Contains(term)));
            }

            var cursos = await query
                .OrderBy(c => c.Ciclo)
                .ThenBy(c => c.NombreCurso)
                .Select(c => new CursoAdminDto
                {
                    IdCurso = c.IdCurso,
                    CodigoCurso = c.CodigoCurso,
                    NombreCurso = c.NombreCurso,
                    Ciclo = c.Ciclo,
                    EstadoActivo = c.EstadoActivo,
                    TotalBasesAsociadas = c.RecursosCursos.Count(),
                    BasesAsociadas = c.RecursosCursos
                        .Where(rc => rc.BaseDatos != null)
                        .Select(rc => rc.BaseDatos.NombreRecurso)
                        .ToList()
                })
                .ToListAsync();

            return Ok(cursos);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var curso = await _context.CursosMedicina
                .Include(c => c.RecursosCursos)
                .ThenInclude(rc => rc.BaseDatos)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCurso == id);

            if (curso == null)
            {
                return NotFound(new { message = "Asignatura no encontrada." });
            }

            var dto = new CursoAdminDto
            {
                IdCurso = curso.IdCurso,
                CodigoCurso = curso.CodigoCurso,
                NombreCurso = curso.NombreCurso,
                Ciclo = curso.Ciclo,
                EstadoActivo = curso.EstadoActivo,
                TotalBasesAsociadas = curso.RecursosCursos.Count,
                BasesAsociadas = curso.RecursosCursos
                    .Where(rc => rc.BaseDatos != null)
                    .Select(rc => rc.BaseDatos.NombreRecurso)
                    .ToList()
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUpdateCursoDto dto)
        {
            if (dto.Ciclo < 1 || dto.Ciclo > 14)
            {
                return BadRequest(new { message = "El ciclo debe estar comprendido entre 1 y 14." });
            }

            if (string.IsNullOrWhiteSpace(dto.NombreCurso))
            {
                return BadRequest(new { message = "El nombre de la asignatura es obligatorio." });
            }

            var codigoCursoLimpio = string.IsNullOrWhiteSpace(dto.CodigoCurso) ? null : dto.CodigoCurso.Trim();
            if (codigoCursoLimpio != null)
            {
                var yaExiste = await _context.CursosMedicina
                    .AnyAsync(c => c.CodigoCurso != null && c.CodigoCurso.ToLower() == codigoCursoLimpio.ToLower());

                if (yaExiste)
                {
                    return BadRequest(new { message = $"El código de asignatura '{codigoCursoLimpio}' ya se encuentra registrado." });
                }
            }

            var nuevoCurso = new CursoMedicina
            {
                CodigoCurso = codigoCursoLimpio,
                NombreCurso = dto.NombreCurso.Trim(),
                Ciclo = dto.Ciclo,
                EstadoActivo = dto.EstadoActivo
            };

            _context.CursosMedicina.Add(nuevoCurso);
            await _context.SaveChangesAsync();

            var resultDto = new CursoAdminDto
            {
                IdCurso = nuevoCurso.IdCurso,
                CodigoCurso = nuevoCurso.CodigoCurso,
                NombreCurso = nuevoCurso.NombreCurso,
                Ciclo = nuevoCurso.Ciclo,
                EstadoActivo = nuevoCurso.EstadoActivo,
                TotalBasesAsociadas = 0,
                BasesAsociadas = new List<string>()
            };

            return CreatedAtAction(nameof(GetById), new { id = nuevoCurso.IdCurso }, resultDto);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateCursoDto dto)
        {
            var curso = await _context.CursosMedicina
                .Include(c => c.RecursosCursos)
                .ThenInclude(rc => rc.BaseDatos)
                .FirstOrDefaultAsync(c => c.IdCurso == id);

            if (curso == null)
            {
                return NotFound(new { message = "Asignatura no encontrada." });
            }

            if (dto.Ciclo < 1 || dto.Ciclo > 14)
            {
                return BadRequest(new { message = "El ciclo debe estar comprendido entre 1 y 14." });
            }

            if (string.IsNullOrWhiteSpace(dto.NombreCurso))
            {
                return BadRequest(new { message = "El nombre de la asignatura es obligatorio." });
            }

            var codigoCursoLimpio = string.IsNullOrWhiteSpace(dto.CodigoCurso) ? null : dto.CodigoCurso.Trim();
            if (codigoCursoLimpio != null)
            {
                var yaExiste = await _context.CursosMedicina
                    .AnyAsync(c => c.IdCurso != id && c.CodigoCurso != null && c.CodigoCurso.ToLower() == codigoCursoLimpio.ToLower());

                if (yaExiste)
                {
                    return BadRequest(new { message = $"El código de asignatura '{codigoCursoLimpio}' ya se encuentra asignado a otra asignatura." });
                }
            }

            curso.CodigoCurso = codigoCursoLimpio;
            curso.NombreCurso = dto.NombreCurso.Trim();
            curso.Ciclo = dto.Ciclo;
            curso.EstadoActivo = dto.EstadoActivo;

            await _context.SaveChangesAsync();

            var resultDto = new CursoAdminDto
            {
                IdCurso = curso.IdCurso,
                CodigoCurso = curso.CodigoCurso,
                NombreCurso = curso.NombreCurso,
                Ciclo = curso.Ciclo,
                EstadoActivo = curso.EstadoActivo,
                TotalBasesAsociadas = curso.RecursosCursos.Count,
                BasesAsociadas = curso.RecursosCursos
                    .Where(rc => rc.BaseDatos != null)
                    .Select(rc => rc.BaseDatos.NombreRecurso)
                    .ToList()
            };

            return Ok(resultDto);
        }

        [HttpPatch("{id:int}/toggle-activo")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var curso = await _context.CursosMedicina.FindAsync(id);
            if (curso == null)
            {
                return NotFound(new { message = "Asignatura no encontrada." });
            }

            curso.EstadoActivo = !curso.EstadoActivo;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = curso.IdCurso,
                estadoActivo = curso.EstadoActivo
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] bool force = false)
        {
            var curso = await _context.CursosMedicina
                .Include(c => c.RecursosCursos)
                .ThenInclude(rc => rc.BaseDatos)
                .FirstOrDefaultAsync(c => c.IdCurso == id);

            if (curso == null)
            {
                return NotFound(new { message = "Asignatura no encontrada." });
            }

            if (curso.RecursosCursos.Any() && !force)
            {
                return Conflict(new
                {
                    message = "No se puede eliminar la asignatura porque está vinculada a bases de datos.",
                    recursosVinculados = curso.RecursosCursos
                        .Where(rc => rc.BaseDatos != null)
                        .Select(rc => rc.BaseDatos.NombreRecurso)
                        .ToList(),
                    puedeDesactivar = true
                });
            }

            if (curso.RecursosCursos.Any())
            {
                _context.RecursosCursos.RemoveRange(curso.RecursosCursos);
            }

            _context.CursosMedicina.Remove(curso);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
