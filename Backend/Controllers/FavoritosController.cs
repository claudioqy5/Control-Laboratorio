using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FavoritosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Favoritos/Alumno/5
        [HttpGet("Alumno/{alumnoId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetFavoritosAlumno(int alumnoId)
        {
            var favoritos = await _context.Favoritos
                .Include(f => f.Libro)
                .Where(f => f.AlumnoID == alumnoId)
                .Select(f => new
                {
                    f.FavoritoID,
                    f.LibroID,
                    LibroTitulo = f.Libro != null ? f.Libro.Titulo : string.Empty,
                    LibroAutor = f.Libro != null ? f.Libro.Autor : string.Empty,
                    LibroPortada = f.Libro != null ? f.Libro.Portada : null
                })
                .ToListAsync();

            return Ok(favoritos);
        }

        // POST: api/Favoritos/Toggle
        [HttpPost("Toggle")]
        public async Task<IActionResult> ToggleFavorito([FromBody] FavoritoDto dto)
        {
            var favoritoExistente = await _context.Favoritos
                .FirstOrDefaultAsync(f => f.AlumnoID == dto.AlumnoID && f.LibroID == dto.LibroID);

            if (favoritoExistente != null)
            {
                _context.Favoritos.Remove(favoritoExistente);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Eliminado de favoritos", estado = false });
            }
            else
            {
                var nuevoFavorito = new Favorito
                {
                    AlumnoID = dto.AlumnoID,
                    LibroID = dto.LibroID
                };
                _context.Favoritos.Add(nuevoFavorito);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Añadido a favoritos", estado = true });
            }
        }
    }

    public class FavoritoDto
    {
        public int AlumnoID { get; set; }
        public int LibroID { get; set; }
    }
}
