using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Categorias
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Categoria>>> GetCategorias()
        {
            return await _context.Categorias.ToListAsync();
        }

        // GET: api/Categorias/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Categoria>> GetCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);

            if (categoria == null)
            {
                return NotFound(new { mensaje = "Categoría no encontrada" });
            }

            return categoria;
        }

        // POST: api/Categorias
        [HttpPost]
        public async Task<ActionResult<Categoria>> PostCategoria(Categoria categoria)
        {
            if (await _context.Categorias.AnyAsync(c => c.Codigo == categoria.Codigo))
            {
                return BadRequest(new { mensaje = "El código de categoría ya existe" });
            }

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategoria), new { id = categoria.CategoriaID }, categoria);
        }

        // PUT: api/Categorias/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategoria(int id, Categoria categoria)
        {
            if (id != categoria.CategoriaID)
            {
                return BadRequest(new { mensaje = "El ID de la categoría no coincide" });
            }

            if (await _context.Categorias.AnyAsync(c => c.Codigo == categoria.Codigo && c.CategoriaID != id))
            {
                return BadRequest(new { mensaje = "El código de categoría ya está asignado a otra categoría" });
            }

            _context.Entry(categoria).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoriaExists(id))
                {
                    return NotFound(new { mensaje = "Categoría no encontrada" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Categorias/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound(new { mensaje = "Categoría no encontrada" });
            }

            // Opcional: Validar si hay libros usando esta categoría
            // (Asumiendo que Categoria es guardada como string en Libro.Categoria, 
            // no hay foreign key estricta. Si se desea, se podría validar:
            // if (await _context.Libros.AnyAsync(l => l.Categoria == categoria.Nombre))
            // { return BadRequest(new { mensaje = "No se puede eliminar la categoría porque hay libros asociados a ella." }); }
            // Por ahora, lo mantenemos simple.)

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Categoría eliminada con éxito" });
        }

        private bool CategoriaExists(int id)
        {
            return _context.Categorias.Any(e => e.CategoriaID == id);
        }
    }
}
