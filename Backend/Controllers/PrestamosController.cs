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

        // GET: api/Prestamos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPrestamos([FromQuery] string? estado = null)
        {
            var query = _context.Prestamos
                .Include(p => p.Alumno)
                .Include(p => p.Libro)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado) && estado.ToLower() != "todos")
            {
                query = query.Where(p => p.Estado == estado);
            }

            var prestamos = await query
                .OrderByDescending(p => p.FechaPrestamo)
                .Select(p => new
                {
                    p.PrestamoID,
                    p.AlumnoID,
                    AlumnoCodigo = p.Alumno != null ? p.Alumno.CodigoUniversitario : string.Empty,
                    AlumnoNombre = p.Alumno != null ? $"{p.Alumno.Nombres} {p.Alumno.ApellidoPaterno} {p.Alumno.ApellidoMaterno}".Trim() : "Desconocido",
                    AlumnoDNI = p.Alumno != null ? p.Alumno.DNI : string.Empty,
                    AlumnoCarrera = p.Alumno != null ? p.Alumno.Carrera : string.Empty,
                    p.LibroID,
                    LibroCodigoBarras = p.Libro != null ? p.Libro.CodigoBarras : string.Empty,
                    LibroTitulo = p.Libro != null ? p.Libro.Titulo : string.Empty,
                    LibroAutor = p.Libro != null ? p.Libro.Autor : string.Empty,
                    LibroUbicacion = p.Libro != null && p.Libro.Estante.HasValue ? $"Estante {p.Libro.Estante}-{p.Libro.Cara ?? "A"} P{p.Libro.Piso ?? 1}" : "Sin asignar",
                    p.FechaPrestamo,
                    p.FechaDevolucion,
                    p.FechaEntregado,
                    p.Estado,
                    EsVencido = p.Estado == "Activo" && DateTime.Now > p.FechaDevolucion,
                    DiasRestantes = (p.FechaDevolucion.Date - DateTime.Now.Date).Days
                })
                .ToListAsync();

            return Ok(prestamos);
        }

        // POST: api/Prestamos/SincronizarAbsys
        [HttpPost("SincronizarAbsys")]
        public async Task<IActionResult> SincronizarAbsys([FromBody] SincronizarAbsysDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.CodigoBarrasLibro))
                {
                    return BadRequest(new { mensaje = "El código de barras del libro es obligatorio" });
                }

                var operacion = (dto.TipoOperacion ?? "PRESTAMO").Trim().ToUpper();

                // 1. Buscar o registrar automáticamente el Libro si no existe
                var libro = await _context.Libros.FirstOrDefaultAsync(l => l.CodigoBarras == dto.CodigoBarrasLibro.Trim());
                if (libro == null)
                {
                    libro = new Libro
                    {
                        CodigoBarras = dto.CodigoBarrasLibro.Trim(),
                        NroRegistro = dto.CodigoBarrasLibro.Trim(),
                        NroClasificacion = "S/C",
                        Titulo = !string.IsNullOrWhiteSpace(dto.TituloLibro) ? dto.TituloLibro.Trim() : $"Libro {dto.CodigoBarrasLibro}",
                        Autor = "Registrado vía AbsysNet",
                        Categoria = "Medicina",
                        Estado = operacion == "PRESTAMO" ? "Prestado" : "Disponible"
                    };
                    _context.Libros.Add(libro);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Actualizar el título si el libro tenía un título genérico o de prueba
                    if (!string.IsNullOrWhiteSpace(dto.TituloLibro) && 
                        !dto.TituloLibro.Contains("Material Bibliográfico") &&
                        (libro.Titulo.Contains("Prueba") || libro.Titulo.StartsWith("Libro ") || libro.Titulo != dto.TituloLibro.Trim()))
                    {
                        libro.Titulo = dto.TituloLibro.Trim();
                        await _context.SaveChangesAsync();
                    }
                }

                // 2. Operación de DEVOLUCIÓN
                if (operacion == "DEVOLUCION")
                {
                    var prestamoActivo = await _context.Prestamos
                        .Include(p => p.Libro)
                        .Include(p => p.Alumno)
                        .Where(p => p.LibroID == libro.LibroID && (p.Estado == "Activo" || p.Estado == "Renovado"))
                        .OrderByDescending(p => p.FechaPrestamo)
                        .FirstOrDefaultAsync();

                    if (prestamoActivo != null)
                    {
                        prestamoActivo.FechaEntregado = DateTime.Now;
                        prestamoActivo.Estado = "Devuelto";
                        libro.Estado = "Disponible";

                        await _context.SaveChangesAsync();
                        return Ok(new
                        {
                            mensaje = "Devolución registrada con éxito en Control-Laboratorio",
                            prestamoId = prestamoActivo.PrestamoID,
                            alumno = prestamoActivo.Alumno != null ? prestamoActivo.Alumno.CodigoUniversitario : "N/A",
                            libro = libro.Titulo
                        });
                    }
                    else
                    {
                        libro.Estado = "Disponible";

                        // Si AbsysNet envió los datos del alumno que devolvió el libro, registramos el préstamo como Devuelto
                        if (!string.IsNullOrWhiteSpace(dto.CodigoAlumno))
                        {
                            var codAlumno = dto.CodigoAlumno.Trim();
                            var alumnoDev = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == codAlumno || a.DNI == codAlumno);
                            if (alumnoDev == null)
                            {
                                string devNombres = "Estudiante";
                                string devApPaterno = "URP";
                                string devApMaterno = "";

                                if (!string.IsNullOrWhiteSpace(dto.NombreAlumno) && dto.NombreAlumno.Contains(','))
                                {
                                    var halves = dto.NombreAlumno.Split(',', 2);
                                    var apellidos = halves[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    devApPaterno = apellidos.Length > 0 ? apellidos[0] : "URP";
                                    devApMaterno = apellidos.Length > 1 ? apellidos[1] : "";
                                    devNombres = halves.Length > 1 ? halves[1].Trim() : "Estudiante";
                                }
                                else
                                {
                                    var nombreParts = (dto.NombreAlumno ?? "Estudiante URP").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    devNombres = nombreParts.Length > 0 ? nombreParts[0] : "Estudiante";
                                    devApPaterno = nombreParts.Length > 1 ? nombreParts[1] : "URP";
                                    devApMaterno = nombreParts.Length > 2 ? nombreParts[2] : "";
                                }

                                alumnoDev = new Alumno
                                {
                                    CodigoUniversitario = codAlumno,
                                    DNI = codAlumno.Length == 8 ? codAlumno : "00000000",
                                    Nombres = devNombres,
                                    ApellidoPaterno = devApPaterno,
                                    ApellidoMaterno = devApMaterno,
                                    Carrera = "Medicina Humana",
                                    Estado = true
                                };
                                _context.Alumnos.Add(alumnoDev);
                                await _context.SaveChangesAsync();
                            }

                            var prestamoDevuelto = new Prestamo
                            {
                                AlumnoID = alumnoDev.AlumnoID,
                                LibroID = libro.LibroID,
                                FechaPrestamo = DateTime.Now.AddDays(-3),
                                FechaDevolucion = dto.FechaDevolucion ?? DateTime.Now,
                                FechaEntregado = DateTime.Now,
                                Estado = "Devuelto"
                            };

                            _context.Prestamos.Add(prestamoDevuelto);
                            await _context.SaveChangesAsync();

                            return Ok(new
                            {
                                mensaje = "Devolución registrada con éxito en Control-Laboratorio",
                                prestamoId = prestamoDevuelto.PrestamoID,
                                alumno = $"{alumnoDev.CodigoUniversitario} - {alumnoDev.Nombres} {alumnoDev.ApellidoPaterno}",
                                libro = libro.Titulo
                            });
                        }

                        await _context.SaveChangesAsync();
                        return Ok(new { mensaje = "El libro se actualizó a Disponible", libro = libro.Titulo });
                    }
                }

                // 3. Operación de PRÉSTAMO
                if (string.IsNullOrWhiteSpace(dto.CodigoAlumno))
                {
                    return BadRequest(new { mensaje = "El código de alumno es requerido para registrar un préstamo" });
                }

                var codLimpio = dto.CodigoAlumno.Trim();
                var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == codLimpio || a.DNI == codLimpio);

                if (alumno == null)
                {
                    // Auto-crear alumno preliminar si aún no estaba en la base de datos de laboratorio
                    string nombres = "Estudiante";
                    string apPaterno = "URP";
                    string apMaterno = "";

                    if (!string.IsNullOrWhiteSpace(dto.NombreAlumno) && dto.NombreAlumno.Contains(','))
                    {
                        var halves = dto.NombreAlumno.Split(',', 2);
                        var apellidos = halves[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        apPaterno = apellidos.Length > 0 ? apellidos[0] : "URP";
                        apMaterno = apellidos.Length > 1 ? apellidos[1] : "";
                        nombres = halves.Length > 1 ? halves[1].Trim() : "Estudiante";
                    }
                    else
                    {
                        var nombreParts = (dto.NombreAlumno ?? "Estudiante URP").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        nombres = nombreParts.Length > 0 ? nombreParts[0] : "Estudiante";
                        apPaterno = nombreParts.Length > 1 ? nombreParts[1] : "URP";
                        apMaterno = nombreParts.Length > 2 ? nombreParts[2] : "";
                    }

                    alumno = new Alumno
                    {
                        CodigoUniversitario = codLimpio,
                        DNI = codLimpio.Length == 8 ? codLimpio : "00000000",
                        Nombres = nombres,
                        ApellidoPaterno = apPaterno,
                        ApellidoMaterno = apMaterno,
                        Carrera = "Medicina Humana",
                        Estado = true
                    };
                    _context.Alumnos.Add(alumno);
                    await _context.SaveChangesAsync();
                }

                // Evitar préstamos duplicados si el script envía la misma lectura en los últimos 2 minutos
                var haceDosMinutos = DateTime.Now.AddMinutes(-2);
                var yaExiste = await _context.Prestamos.AnyAsync(p => 
                    p.AlumnoID == alumno.AlumnoID && 
                    p.LibroID == libro.LibroID && 
                    p.Estado == "Activo" && 
                    p.FechaPrestamo >= haceDosMinutos);

                if (yaExiste)
                {
                    return Ok(new { mensaje = "El préstamo ya fue registrado previamente", libro = libro.Titulo, alumno = alumno.CodigoUniversitario });
                }

                // Fecha de devolución (3 días hábiles o la fecha enviada de AbsysNet)
                var fechaDev = dto.FechaDevolucion.HasValue && dto.FechaDevolucion.Value > DateTime.Now 
                    ? dto.FechaDevolucion.Value 
                    : DateTime.Now.AddDays(3);

                var nuevoPrestamo = new Prestamo
                {
                    AlumnoID = alumno.AlumnoID,
                    LibroID = libro.LibroID,
                    FechaPrestamo = DateTime.Now,
                    FechaDevolucion = fechaDev,
                    Estado = "Activo"
                };

                libro.Estado = "Prestado";
                _context.Prestamos.Add(nuevoPrestamo);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Préstamo sincronizado con éxito desde AbsysNet",
                    prestamoId = nuevoPrestamo.PrestamoID,
                    alumno = $"{alumno.CodigoUniversitario} - {alumno.Nombres} {alumno.ApellidoPaterno}",
                    libro = $"{libro.CodigoBarras} - {libro.Titulo}",
                    fechaDevolucion = nuevoPrestamo.FechaDevolucion
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    error = "Error al sincronizar con AbsysNet", 
                    detalle = ex.Message, 
                    inner = ex.InnerException?.Message 
                });
            }
        }

        // GET: api/Prestamos/Ping
        [HttpGet("Ping")]
        public IActionResult Ping()
        {
            return Ok(new { mensaje = "Conexión exitosa con el servidor de Control-Laboratorio", fecha = DateTime.Now });
        }

        // DELETE: api/Prestamos/LimpiarPruebas
        [HttpDelete("LimpiarPruebas")]
        public async Task<IActionResult> LimpiarPruebas()
        {
            var prestamosTest = await _context.Prestamos
                .Include(p => p.Libro)
                .Include(p => p.Alumno)
                .Include(p => p.Multas)
                .Where(p => 
                    (p.Alumno != null && (p.Alumno.Nombres.Contains("Prueba") || p.Alumno.CodigoUniversitario == "202110452")) ||
                    (p.Libro != null && (p.Libro.CodigoBarras == "TEST-001" || p.Libro.Titulo.Contains("Prueba"))))
                .ToListAsync();

            foreach (var p in prestamosTest)
            {
                if (p.Libro != null) p.Libro.Estado = "Disponible";
                if (p.Multas.Any()) _context.Multas.RemoveRange(p.Multas);
            }

            _context.Prestamos.RemoveRange(prestamosTest);

            var alumnoTest = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == "202110452");
            if (alumnoTest != null) _context.Alumnos.Remove(alumnoTest);

            var libroTest = await _context.Libros.FirstOrDefaultAsync(l => l.CodigoBarras == "TEST-001");
            if (libroTest != null) _context.Libros.Remove(libroTest);

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Datos de prueba eliminados correctamente" });
        }

        // DELETE: api/Prestamos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarPrestamo(int id)
        {
            var prestamo = await _context.Prestamos
                .Include(p => p.Libro)
                .Include(p => p.Multas)
                .FirstOrDefaultAsync(p => p.PrestamoID == id);

            if (prestamo == null)
            {
                return NotFound(new { mensaje = "Préstamo no encontrado" });
            }

            if (prestamo.Libro != null && prestamo.Estado == "Activo")
            {
                prestamo.Libro.Estado = "Disponible";
            }

            if (prestamo.Multas.Any())
            {
                _context.Multas.RemoveRange(prestamo.Multas);
            }

            _context.Prestamos.Remove(prestamo);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Préstamo eliminado con éxito" });
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

    public class SincronizarAbsysDto
    {
        public string TipoOperacion { get; set; } = "PRESTAMO"; // PRESTAMO o DEVOLUCION
        public string CodigoAlumno { get; set; } = string.Empty;
        public string? NombreAlumno { get; set; }
        public string CodigoBarrasLibro { get; set; } = string.Empty;
        public string? TituloLibro { get; set; }
        public DateTime? FechaDevolucion { get; set; }
    }
}
