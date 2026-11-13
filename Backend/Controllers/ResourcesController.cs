using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.DTOs;
using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/resources")]
    public class ResourcesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResourcesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? ciclo,
            [FromQuery] bool? esSuscripcion,
            [FromQuery] bool? tieneAppMovil,
            [FromQuery] string? search,
            [FromQuery] bool? destacadosOnly)
        {
            var query = _context.BasesDatosMedicas
                .Include(r => r.RecursosCursos)
                .ThenInclude(rc => rc.Curso)
                .Where(r => r.EstadoActivo)
                .AsNoTracking()
                .AsQueryable();

            if (ciclo.HasValue)
            {
                query = query.Where(r => r.RecursosCursos.Any(rc => rc.Curso.Ciclo == ciclo.Value));
            }

            if (esSuscripcion.HasValue)
            {
                query = query.Where(r => r.EsSuscripcion == esSuscripcion.Value);
            }

            if (tieneAppMovil.HasValue)
            {
                query = query.Where(r => r.TieneAppMovil == tieneAppMovil.Value);
            }

            if (destacadosOnly == true)
            {
                query = query.Where(r => r.MostrarEnHexagonos);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(r => r.NombreRecurso.ToLower().Contains(term)
                    || r.DescripcionClinica.ToLower().Contains(term)
                    || r.RecursosCursos.Any(rc => rc.Curso.NombreCurso.ToLower().Contains(term)));
            }

            var resources = await query
                .OrderByDescending(r => r.MostrarEnHexagonos)
                .ThenBy(r => r.NombreRecurso)
                .Select(r => MapToPublicDto(r))
                .ToListAsync();

            return Ok(resources);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var resource = await _context.BasesDatosMedicas
                .Include(r => r.RecursosCursos)
                .ThenInclude(rc => rc.Curso)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdBaseDatos == id && r.EstadoActivo);

            if (resource == null)
            {
                return NotFound(new { message = "Recurso no encontrado o no disponible." });
            }

            return Ok(MapToPublicDto(resource));
        }

        [HttpGet("destacados")]
        public async Task<IActionResult> GetDestacados()
        {
            var destacados = await _context.BasesDatosMedicas
                .Include(r => r.RecursosCursos)
                .ThenInclude(rc => rc.Curso)
                .Where(r => r.EstadoActivo && r.MostrarEnHexagonos)
                .OrderBy(r => r.NombreRecurso)
                .Take(6)
                .Select(r => MapToPublicDto(r))
                .AsNoTracking()
                .ToListAsync();

            return Ok(destacados);
        }

        [HttpGet("cursos")]
        public async Task<IActionResult> GetCursos()
        {
            var cursos = await _context.CursosMedicina
                .OrderBy(c => c.Ciclo)
                .ThenBy(c => c.NombreCurso)
                .Select(c => new CursoDto
                {
                    IdCurso = c.IdCurso,
                    NombreCurso = c.NombreCurso,
                    Ciclo = c.Ciclo,
                    CodigoCurso = c.CodigoCurso
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(cursos);
        }

        private static ResourcePublicDto MapToPublicDto(BaseDatosMedica r)
        {
            return new ResourcePublicDto
            {
                IdBaseDatos = r.IdBaseDatos,
                NombreRecurso = r.NombreRecurso,
                LogotipoUrl = r.LogotipoUrl,
                DescripcionClinica = r.DescripcionClinica,
                EsSuscripcion = r.EsSuscripcion,
                TieneAppMovil = r.TieneAppMovil,
                UrlExterno = r.UrlExterno,
                EstadoActivo = r.EstadoActivo,
                MostrarEnHexagonos = r.MostrarEnHexagonos,
                YoutubeVideoId = r.YoutubeVideoId,
                Cursos = r.RecursosCursos
                    .OrderBy(rc => rc.Curso.Ciclo)
                    .ThenBy(rc => rc.Curso.NombreCurso)
                    .Select(rc => new ResourceCourseItemDto
                    {
                        IdCurso = rc.Curso.IdCurso,
                        NombreCurso = rc.Curso.NombreCurso,
                        Ciclo = rc.Curso.Ciclo,
                        CodigoCurso = rc.Curso.CodigoCurso
                    })
                    .ToList()
            };
        }
    }
}
