using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using ControlLaboratorio.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlumnosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AbysScraperService _abys;

        public AlumnosController(ApplicationDbContext context, AbysScraperService abys)
        {
            _context = context;
            _abys = abys;
        }

        // ── NUEVO: Buscar alumno en AbsysNet por código de barras ─────────────────────────
        /// <summary>
        /// Busca los datos de un alumno en el sistema de la Biblioteca URP (AbsysNet)
        /// a partir del código escaneado en su carnet universitario.
        /// Se llama automáticamente cuando se escanea un carnet y el alumno NO existe aún
        /// en el sistema de control de laboratorio.
        /// </summary>
        [HttpGet("buscar-biblioteca/{codigo}")]
        public async Task<IActionResult> BuscarEnBiblioteca(string codigo)
        {
            var resultado = await _abys.BuscarPorCodigoAsync(codigo);
            if (resultado == null)
                return NotFound(new { mensaje = "Alumno no encontrado en el sistema de biblioteca." });
            return Ok(resultado);
        }
        // ─────────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Alumno>>> GetAlumnos()
        {
            return await _context.Alumnos.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Alumno>> GetAlumno(int id)
        {
            var alumno = await _context.Alumnos.FindAsync(id);
            if (alumno == null) return NotFound();
            return alumno;
        }

        [HttpPost]
        public async Task<ActionResult<Alumno>> PostAlumno(Alumno alumno)
        {
            _context.Alumnos.Add(alumno);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAlumno), new { id = alumno.AlumnoID }, alumno);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlumno(int id, Alumno alumno)
        {
            if (id != alumno.AlumnoID) return BadRequest();
            _context.Entry(alumno).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlumno(int id)
        {
            var alumno = await _context.Alumnos.FindAsync(id);
            if (alumno == null) return NotFound();
            _context.Alumnos.Remove(alumno);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkImport(List<Alumno> alumnos)
        {
            if (alumnos == null || alumnos.Count == 0) return BadRequest("Lista vacía");

            var codigosExistentes = await _context.Alumnos
                .Select(a => a.CodigoUniversitario)
                .ToListAsync();

            var nuevosAlumnos = alumnos
                .Where(a => !codigosExistentes.Contains(a.CodigoUniversitario))
                .ToList();

            if (nuevosAlumnos.Count > 0)
            {
                _context.Alumnos.AddRange(nuevosAlumnos);
                await _context.SaveChangesAsync();
            }

            return Ok(new { 
                procesados = alumnos.Count, 
                insertados = nuevosAlumnos.Count, 
                omitidos = alumnos.Count - nuevosAlumnos.Count 
            });
        }
    }
}
