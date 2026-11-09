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
                                    CorreoInstitucional = $"{codAlumno}@urp.edu.pe",
                                    Estado = true
                                };
                                _context.Alumnos.Add(alumnoDev);
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(alumnoDev.CorreoInstitucional) && !string.IsNullOrWhiteSpace(alumnoDev.CodigoUniversitario))
                                {
                                    alumnoDev.CorreoInstitucional = $"{alumnoDev.CodigoUniversitario.Trim()}@urp.edu.pe";
                                    await _context.SaveChangesAsync();
                                }
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
                        CorreoInstitucional = $"{codLimpio}@urp.edu.pe",
                        Estado = true
                    };
                    _context.Alumnos.Add(alumno);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(alumno.CorreoInstitucional) && !string.IsNullOrWhiteSpace(alumno.CodigoUniversitario))
                    {
                        alumno.CorreoInstitucional = $"{alumno.CodigoUniversitario.Trim()}@urp.edu.pe";
                        await _context.SaveChangesAsync();
                    }
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

        // GET: api/Prestamos/Estadisticas
        [HttpGet("Estadisticas")]
        public async Task<IActionResult> GetEstadisticasPrestamos(
            [FromQuery] DateTime? date,
            [FromQuery] int? month,
            [FromQuery] int? year,
            [FromQuery] string? mode = null)
        {
            var now = DateTime.Now;
            var targetDate = date?.Date ?? now.Date;

            DateTime rangeStart;
            DateTime rangeEnd;

            if (mode == "date" && date.HasValue)
            {
                rangeStart = date.Value.Date;
                rangeEnd = rangeStart.AddDays(1);
            }
            else if (mode == "year" && year.HasValue)
            {
                rangeStart = new DateTime(year.Value, 1, 1);
                rangeEnd = rangeStart.AddYears(1);
            }
            else
            {
                // Modo mes por defecto
                var targetYear = year ?? now.Year;
                var targetMonth = month ?? now.Month;
                rangeStart = new DateTime(targetYear, targetMonth, 1);
                rangeEnd = rangeStart.AddMonths(1);
            }

            // 1. KPIs Generales (Globales y del período)
            var totalActivos = await _context.Prestamos.CountAsync(p => p.Estado == "Activo" || p.Estado == "Renovado");
            var totalVencidos = await _context.Prestamos.CountAsync(p => (p.Estado == "Activo" || p.Estado == "Renovado") && p.FechaDevolucion < now);
            var porVencerHoy = await _context.Prestamos.CountAsync(p => (p.Estado == "Activo" || p.Estado == "Renovado") && p.FechaDevolucion.Date == now.Date);

            // Préstamos creados dentro del rango filtrado
            var prestamosEnPeriodo = await _context.Prestamos
                .Include(p => p.Libro)
                .Include(p => p.Alumno)
                .Where(p => p.FechaPrestamo >= rangeStart && p.FechaPrestamo < rangeEnd)
                .ToListAsync();

            // Devoluciones procesadas dentro del rango filtrado
            var devolucionesEnPeriodo = await _context.Prestamos
                .Include(p => p.Libro)
                .Include(p => p.Alumno)
                .Where(p => p.FechaEntregado.HasValue && p.FechaEntregado.Value >= rangeStart && p.FechaEntregado.Value < rangeEnd)
                .ToListAsync();

            var totalPrestamosPeriodo = prestamosEnPeriodo.Count;
            var totalDevueltosPeriodo = devolucionesEnPeriodo.Count;

            // Tasa de devolución a tiempo en el período
            var devueltosConFecha = devolucionesEnPeriodo.Where(p => p.FechaEntregado.HasValue).ToList();
            var devueltosATiempo = devueltosConFecha.Count(p => p.FechaEntregado!.Value <= p.FechaDevolucion);
            var tasaATiempo = devueltosConFecha.Any() 
                ? (int)Math.Round((double)devueltosATiempo / devueltosConFecha.Count * 100) 
                : 100;

            // Catálogo disponible vs en préstamo
            var totalCatalogo = await _context.Libros.CountAsync();
            var librosPrestadosActualmente = await _context.Libros.CountAsync(l => l.Estado == "Prestado");

            // 2. Flujo cronológico (Días si es mes, meses si es año, u horas si es día)
            object tendenciaFlujo;
            if (mode == "date")
            {
                // Agrupado por horas (7 AM a 9 PM)
                tendenciaFlujo = Enumerable.Range(7, 15).Select(h => new
                {
                    Etiqueta = $"{h:00}:00",
                    Prestamos = prestamosEnPeriodo.Count(p => p.FechaPrestamo.Hour == h),
                    Devoluciones = devolucionesEnPeriodo.Count(p => p.FechaEntregado!.Value.Hour == h)
                }).ToList();
            }
            else if (mode == "year")
            {
                // Agrupado por los 12 meses
                var mesesNombres = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Set", "Oct", "Nov", "Dic" };
                tendenciaFlujo = Enumerable.Range(1, 12).Select(m => new
                {
                    Etiqueta = mesesNombres[m - 1],
                    Prestamos = prestamosEnPeriodo.Count(p => p.FechaPrestamo.Month == m),
                    Devoluciones = devolucionesEnPeriodo.Count(p => p.FechaEntregado!.Value.Month == m)
                }).ToList();
            }
            else
            {
                // Modo mes: Agrupado por días del mes
                var diasEnMes = DateTime.DaysInMonth(rangeStart.Year, rangeStart.Month);
                tendenciaFlujo = Enumerable.Range(1, diasEnMes).Select(d => new
                {
                    Etiqueta = $"Día {d}",
                    Prestamos = prestamosEnPeriodo.Count(p => p.FechaPrestamo.Day == d),
                    Devoluciones = devolucionesEnPeriodo.Count(p => p.FechaEntregado!.Value.Day == d)
                }).ToList();
            }

            // 3. Top 10 Libros Más Solicitados en el período
            var topLibros = prestamosEnPeriodo
                .Where(p => p.Libro != null)
                .GroupBy(p => new { p.LibroID, p.Libro!.Titulo, p.Libro.CodigoBarras })
                .Select(g => new
                {
                    LibroID = g.Key.LibroID,
                    Titulo = g.Key.Titulo,
                    CodigoBarras = g.Key.CodigoBarras,
                    Total = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToList();

            // 4. Distribución por Categoría Médica
            var porCategoria = prestamosEnPeriodo
                .Where(p => p.Libro != null)
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Libro!.Categoria) ? "General / Sin Categoría" : p.Libro.Categoria.Trim())
                .Select(g => new
                {
                    Categoria = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(8)
                .ToList();

            // 5. Horarios de mayor afluencia (Horas punta 08:00 a 20:00)
            var horasPunta = Enumerable.Range(8, 13).Select(h => new
            {
                Hora = $"{h:00}:00",
                TotalMovimientos = prestamosEnPeriodo.Count(p => p.FechaPrestamo.Hour == h) +
                                   devolucionesEnPeriodo.Count(p => p.FechaEntregado!.Value.Hour == h)
            }).ToList();

            // 6. Estado de Cumplimiento / Morosidad en el período
            var devueltosEnTiempo = devueltosATiempo;
            var devueltosConRetraso = devueltosConFecha.Count - devueltosATiempo;
            var aunActivosRegular = prestamosEnPeriodo.Count(p => (p.Estado == "Activo" || p.Estado == "Renovado") && p.FechaDevolucion >= now);
            var aunActivosVencidos = prestamosEnPeriodo.Count(p => (p.Estado == "Activo" || p.Estado == "Renovado") && p.FechaDevolucion < now);

            // 7. Top 5 Alumnos Lectores (Ranking en el período)
            var topAlumnos = prestamosEnPeriodo
                .Where(p => p.Alumno != null)
                .GroupBy(p => new
                {
                    p.AlumnoID,
                    p.Alumno!.CodigoUniversitario,
                    NombreCompleto = $"{p.Alumno.Nombres} {p.Alumno.ApellidoPaterno}".Trim(),
                    p.Alumno.Carrera
                })
                .Select(g => new
                {
                    AlumnoID = g.Key.AlumnoID,
                    CodigoUniversitario = g.Key.CodigoUniversitario,
                    Nombre = g.Key.NombreCompleto,
                    Carrera = g.Key.Carrera,
                    TotalPrestamos = g.Count()
                })
                .OrderByDescending(x => x.TotalPrestamos)
                .Take(5)
                .ToList();

            return Ok(new
            {
                Kpis = new
                {
                    TotalActivos = totalActivos,
                    TotalVencidos = totalVencidos,
                    PorVencerHoy = porVencerHoy,
                    TasaATiempo = tasaATiempo,
                    TotalPrestamosPeriodo = totalPrestamosPeriodo,
                    TotalDevueltosPeriodo = totalDevueltosPeriodo,
                    TotalCatalogo = totalCatalogo,
                    LibrosPrestados = librosPrestadosActualmente
                },
                TendenciaFlujo = tendenciaFlujo,
                TopLibros = topLibros,
                PorCategoria = porCategoria,
                HorasPunta = horasPunta,
                Cumplimiento = new
                {
                    DevueltosATiempo = devueltosEnTiempo,
                    DevueltosConRetraso = devueltosConRetraso,
                    ActivosEnPlazo = aunActivosRegular,
                    ActivosVencidos = aunActivosVencidos
                },
                TopAlumnos = topAlumnos
            });
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
