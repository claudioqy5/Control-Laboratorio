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

        public AlumnosController(ApplicationDbContext context)
        {
            _context = context;
        }

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
            var existeAlumno = await _context.Alumnos.AnyAsync(a => a.CodigoUniversitario == alumno.CodigoUniversitario || a.DNI == alumno.DNI);
            if (existeAlumno)
            {
                return Conflict(new { message = $"Ya existe un alumno registrado con el código {alumno.CodigoUniversitario} o DNI {alumno.DNI}." });
            }

            _context.Alumnos.Add(alumno);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAlumno), new { id = alumno.AlumnoID }, alumno);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlumno(int id, Alumno alumno)
        {
            if (id != alumno.AlumnoID) return BadRequest();

            var existeAlumno = await _context.Alumnos.AnyAsync(a => (a.CodigoUniversitario == alumno.CodigoUniversitario || a.DNI == alumno.DNI) && a.AlumnoID != id);
            if (existeAlumno)
            {
                return Conflict(new { message = $"Ya existe otro alumno registrado con el código {alumno.CodigoUniversitario} o DNI {alumno.DNI}." });
            }

            var alumnoDb = await _context.Alumnos.AsNoTracking().FirstOrDefaultAsync(a => a.AlumnoID == id);
            if (alumnoDb == null) return NotFound();

            // Si pasa de inactivo (false) a activo (true), es una reactivación autorizada
            if (!alumnoDb.Estado && alumno.Estado)
            {
                alumno.LimiteDiarioSegundos = (alumnoDb.LimiteDiarioSegundos < 10800 ? 10800 : alumnoDb.LimiteDiarioSegundos) + 10800;
            }

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
