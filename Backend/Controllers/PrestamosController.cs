using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrestamosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PrestamosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Prestamos/Alumno/5
        [HttpGet("Alumno/{alumnoId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPrestamosAlumno(int alumnoId)
        {
            var prestamos = await _context.Prestamos
                .Include(p => p.Libro)
                .Where(p => p.AlumnoID == alumnoId)
                .Select(p => new
                {
                    p.PrestamoID,
                    p.LibroID,
                    LibroTitulo = p.Libro != null ? p.Libro.Titulo : string.Empty,
                    LibroAutor = p.Libro != null ? p.Libro.Autor : string.Empty,
                    LibroPortada = p.Libro != null ? p.Libro.Portada : null,
                    p.FechaPrestamo,
                    p.FechaDevolucion,
                    p.FechaEntregado,
                    p.Estado,
                    DiasRestantes = (p.FechaDevolucion - DateTime.Now).Days
                })
                .ToListAsync();

            return Ok(prestamos);
        }

        // POST: api/Prestamos/Solicitar
        [HttpPost("Solicitar")]
        public async Task<IActionResult> SolicitarPrestamo([FromBody] SolicitudPrestamoDto dto)
        {
            var alumno = await _context.Alumnos.FindAsync(dto.AlumnoID);
            if (alumno == null)
            {
                return NotFound(new { mensaje = "Alumno no encontrado" });
            }

            var libro = await _context.Libros.FindAsync(dto.LibroID);
            if (libro == null)
            {
                return NotFound(new { mensaje = "Libro no encontrado" });
            }

            if (libro.Estado != "Disponible")
            {
                return BadRequest(new { mensaje = "El libro no se encuentra disponible para préstamo" });
            }

            // Crear el préstamo
            var nuevoPrestamo = new Prestamo
            {
                AlumnoID = dto.AlumnoID,
                LibroID = dto.LibroID,
                FechaPrestamo = DateTime.Now,
                FechaDevolucion = DateTime.Now.AddDays(14), // 14 días de préstamo por defecto
                Estado = "Activo"
            };

            // Cambiar estado de libro
            libro.Estado = "Prestado";

            _context.Prestamos.Add(nuevoPrestamo);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Préstamo solicitado con éxito",
                prestamoId = nuevoPrestamo.PrestamoID,
                fechaDevolucion = nuevoPrestamo.FechaDevolucion
            });
        }

        // POST: api/Prestamos/Renovar/5
        [HttpPost("Renovar/{id}")]
        public async Task<IActionResult> RenovarPrestamo(int id)
        {
            var prestamo = await _context.Prestamos.FindAsync(id);
            if (prestamo == null)
            {
                return NotFound(new { mensaje = "Préstamo no encontrado" });
            }

            if (prestamo.Estado != "Activo" && prestamo.Estado != "Renovado")
            {
                return BadRequest(new { mensaje = "Solo se pueden renovar préstamos activos" });
            }

            // Sumar 7 días a la fecha de devolución
            prestamo.FechaDevolucion = prestamo.FechaDevolucion.AddDays(7);
            prestamo.Estado = "Renovado";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Préstamo renovado con éxito",
                nuevaFechaDevolucion = prestamo.FechaDevolucion
            });
        }

        // POST: api/Prestamos/Devolver/5
        [HttpPost("Devolver/{id}")]
        public async Task<IActionResult> DevolverLibro(int id)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Libro)
                .FirstOrDefaultAsync(p => p.PrestamoID == id);

            if (prestamo == null)
            {
                return NotFound(new { mensaje = "Préstamo no encontrado" });
            }

            if (prestamo.Estado == "Devuelto")
            {
                return BadRequest(new { mensaje = "El libro ya fue devuelto anteriormente" });
            }

            prestamo.FechaEntregado = DateTime.Now;
            prestamo.Estado = "Devuelto";

            if (prestamo.Libro != null)
            {
                prestamo.Libro.Estado = "Disponible";
            }

            // Generar multa si se pasó de la fecha de devolución
            if (DateTime.Now > prestamo.FechaDevolucion)
            {
                var diasRetraso = (DateTime.Now - prestamo.FechaDevolucion).Days;
                var montoMulta = diasRetraso * 1.50m; // 1.50 soles/dólares por día de retraso

                var multa = new Multa
                {
                    AlumnoID = prestamo.AlumnoID,
                    PrestamoID = prestamo.PrestamoID,
                    Monto = montoMulta,
                    Estado = "Pendiente",
                    FechaEmision = DateTime.Now
                };

                _context.Multas.Add(multa);
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Libro devuelto con éxito" });
        }
    }

    public class SolicitudPrestamoDto
    {
        public int AlumnoID { get; set; }
        public int LibroID { get; set; }
    }
}
