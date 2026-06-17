using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LibrosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LibrosController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/Libros
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Libro>>> GetLibros()
        {
            return await _context.Libros.ToListAsync();
        }

        // GET: api/Libros/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Libro>> GetLibro(int id)
        {
            var libro = await _context.Libros.FindAsync(id);

            if (libro == null)
            {
                return NotFound(new { mensaje = "Libro no encontrado" });
            }

            return libro;
        }

        // POST: api/Libros
        [HttpPost]
        public async Task<ActionResult<Libro>> PostLibro(Libro libro)
        {
            if (await _context.Libros.AnyAsync(l => l.NroRegistro == libro.NroRegistro))
            {
                return BadRequest(new { mensaje = "El número de registro ya existe" });
            }

            if (await _context.Libros.AnyAsync(l => l.CodigoBarras == libro.CodigoBarras))
            {
                return BadRequest(new { mensaje = "El código de barras ya existe" });
            }

            _context.Libros.Add(libro);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLibro), new { id = libro.LibroID }, libro);
        }

        // PUT: api/Libros/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLibro(int id, Libro libro)
        {
            if (id != libro.LibroID)
            {
                return BadRequest(new { mensaje = "El ID del libro no coincide" });
            }

            // Validar unicidad de NroRegistro y CodigoBarras excluyendo el libro actual
            if (await _context.Libros.AnyAsync(l => l.NroRegistro == libro.NroRegistro && l.LibroID != id))
            {
                return BadRequest(new { mensaje = "El número de registro ya está asignado a otro libro" });
            }

            if (await _context.Libros.AnyAsync(l => l.CodigoBarras == libro.CodigoBarras && l.LibroID != id))
            {
                return BadRequest(new { mensaje = "El código de barras ya está asignado a otro libro" });
            }

            _context.Entry(libro).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LibroExists(id))
                {
                    return NotFound(new { mensaje = "Libro no encontrado" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Libros/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLibro(int id)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
            {
                return NotFound(new { mensaje = "Libro no encontrado" });
            }

            _context.Libros.Remove(libro);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Libro eliminado con éxito" });
        }

        // POST: api/Libros/5/Portada
        [HttpPost("{id}/Portada")]
        public async Task<IActionResult> UploadPortada(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { mensaje = "No se proporcionó ningún archivo." });

            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
                return NotFound(new { mensaje = "Libro no encontrado" });

            // Ensure the directory exists. Fallback to ContentRootPath/wwwroot if WebRootPath is null.
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "portadas", "biblioteca");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate a unique filename using a GUID and original extension
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update the DB with the relative URL using the new /api/static prefix
            var relativeUrl = $"/api/static/portadas/biblioteca/{uniqueFileName}";
            libro.Portada = relativeUrl;

            await _context.SaveChangesAsync();

            return Ok(new { 
                mensaje = "Portada subida con éxito", 
                portadaUrl = relativeUrl 
            });
        }

        private bool LibroExists(int id)
        {
            return _context.Libros.Any(e => e.LibroID == id);
        }
    }
}
