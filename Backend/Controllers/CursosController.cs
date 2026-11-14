using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.DTOs;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/cursos")]
    public class CursosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CursosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCursos([FromQuery] int? ciclo)
        {
            var query = _context.CursosMedicina
                .AsNoTracking()
                .Where(c => c.EstadoActivo);

            if (ciclo.HasValue)
            {
                query = query.Where(c => c.Ciclo == ciclo.Value);
            }

            var cursos = await query
                .OrderBy(c => c.Ciclo)
                .ThenBy(c => c.NombreCurso)
                .Select(c => new CursoDto
                {
                    IdCurso = c.IdCurso,
                    NombreCurso = c.NombreCurso,
                    Ciclo = c.Ciclo,
                    CodigoCurso = c.CodigoCurso
                })
                .ToListAsync();

            return Ok(cursos);
        }
    }
}
