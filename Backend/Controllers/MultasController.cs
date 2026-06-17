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
    public class MultasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MultasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Multas/Alumno/5
        [HttpGet("Alumno/{alumnoId}")]
        public async Task<ActionResult<IEnumerable<Multa>>> GetMultasAlumno(int alumnoId)
        {
            return await _context.Multas
                .Where(m => m.AlumnoID == alumnoId)
                .OrderByDescending(m => m.FechaEmision)
                .ToListAsync();
        }

        // POST: api/Multas/Pagar/5
        [HttpPost("Pagar/{id}")]
        public async Task<IActionResult> PagarMulta(int id)
        {
            var multa = await _context.Multas.FindAsync(id);
            if (multa == null)
            {
                return NotFound(new { mensaje = "Multa no encontrada" });
            }

            if (multa.Estado == "Pagado")
            {
                return BadRequest(new { mensaje = "La multa ya se encuentra pagada" });
            }

            multa.Estado = "Pagado";
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Multa pagada con éxito" });
        }
    }
}
