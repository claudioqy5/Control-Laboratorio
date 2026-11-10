using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BotController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Bot/Contexto
        // Proporciona el estado real en vivo de la BD y la base de conocimiento del sistema para n8n y Gemini
        [HttpGet("Contexto")]
        public async Task<IActionResult> GetContextoEnVivo()
        {
            var now = TimeHelper.GetPeruTime();
            var today = now.Date;

            // 1. Métricas de Biblioteca y Préstamos
            var totalCatalogo = await _context.Libros.CountAsync();
            var librosPrestadosActualmente = await _context.Libros.CountAsync(l => l.Estado == "Prestado");
            var librosDisponibles = totalCatalogo - librosPrestadosActualmente;

            var prestamosHoy = await _context.Prestamos
                .Where(p => p.FechaPrestamo.Date == today)
                .CountAsync();

            var devolucionesHoy = await _context.Prestamos
                .Where(p => p.FechaEntregado.HasValue && p.FechaEntregado.Value.Date == today)
                .CountAsync();

            var prestamosActivos = await _context.Prestamos
                .Where(p => p.Estado == "Activo" || p.Estado == "Renovado")
                .CountAsync();

            var prestamosVencidos = await _context.Prestamos
                .Where(p => (p.Estado == "Activo" || p.Estado == "Renovado") && p.FechaDevolucion < now)
                .CountAsync();

            var porVencerHoy = await _context.Prestamos
                .Where(p => (p.Estado == "Activo" || p.Estado == "Renovado") && p.FechaDevolucion.Date == today)
                .CountAsync();

            // Tasa de cumplimiento del día
            var devueltosHoyList = await _context.Prestamos
                .Where(p => p.FechaEntregado.HasValue && p.FechaEntregado.Value.Date == today)
                .ToListAsync();
            var devueltosATiempoHoy = devueltosHoyList.Count(p => p.FechaEntregado!.Value <= p.FechaDevolucion);
            var tasaATiempoHoy = devueltosHoyList.Any()
                ? (int)Math.Round((double)devueltosATiempoHoy / devueltosHoyList.Count * 100)
                : 100;

            // Libros más prestados hoy
            var topLibrosHoy = await _context.Prestamos
                .Where(p => p.FechaPrestamo.Date == today && p.Libro != null)
                .GroupBy(p => p.Libro!.Titulo)
                .Select(g => new { Titulo = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToListAsync();

            // Últimos 5 movimientos de hoy
            var ultimosMovimientos = await _context.Prestamos
                .Include(p => p.Libro)
                .Include(p => p.Alumno)
                .Where(p => p.FechaPrestamo.Date == today || (p.FechaEntregado.HasValue && p.FechaEntregado.Value.Date == today))
                .OrderByDescending(p => p.FechaEntregado ?? p.FechaPrestamo)
                .Take(5)
                .Select(p => new
                {
                    Tipo = p.FechaEntregado.HasValue && p.FechaEntregado.Value.Date == today ? "Devolución" : "Préstamo",
                    Libro = p.Libro != null ? p.Libro.Titulo : "Sin título",
                    Alumno = p.Alumno != null ? $"{p.Alumno.Nombres} {p.Alumno.ApellidoPaterno}".Trim() : "Desconocido",
                    Hora = (p.FechaEntregado ?? p.FechaPrestamo).ToString("HH:mm")
                })
                .ToListAsync();

            // 2. Métricas de Laboratorio de Cómputo
            var totalEquipos = await _context.Equipos.CountAsync();
            var equiposHabilitados = await _context.Equipos.CountAsync(e => e.Estado);

            var sesionesActivas = await _context.Sesiones
                .Include(s => s.Equipo)
                .Where(s => s.HoraFin == null)
                .ToListAsync();

            var equiposOcupados = sesionesActivas.Select(s => s.EquipoID).Distinct().Count();
            var equiposLibres = Math.Max(0, equiposHabilitados - equiposOcupados);

            var sesionesHoy = await _context.Sesiones
                .Where(s => s.Fecha.Date == today)
                .CountAsync();

            // 3. Alumnos y Multas
            var totalAlumnos = await _context.Alumnos.CountAsync();
            var multasPendientes = await _context.Multas.CountAsync(m => m.Estado == "Pendiente");

            // 4. Base de Conocimiento Estructurada de las Secciones del Sistema
            var seccionesSistema = new
            {
                ResumenDiario = "Muestra en tiempo real el estado del Laboratorio de Cómputo hoy: sesiones activas, porcentaje de ocupación, computadoras libres y afluencia por hora.",
                EstadisticasEquipos = "Analítica histórica del uso de computadoras en el laboratorio: gráficos de horas acumuladas, asistencia por semana, mes o año, y exportación de reportes.",
                EnLinea = "Monitoreo en tiempo real de los alumnos conectados actualmente en las estaciones de cómputo, con tiempo transcurrido y nombre del equipo.",
                MapaEstaciones = "Mapa interactivo en cuadrícula que representa la distribución física de las 67 computadoras del laboratorio, mostrando en colores si están Libres (verde), Ocupadas (rojo) o en Mantenimiento.",
                Computadoras = "Gestión del inventario de equipos de cómputo: nombres de red, direcciones IP, alias, estado operativo y asignaciones de estación.",
                Participantes = "Padrón de estudiantes de Medicina: registro de códigos universitarios, carreras, historial de visitas, límites de tiempo diario y control de multas/sanciones.",
                Reportes = "Módulo oficial para generar y descargar reportes ejecutivos en formato Excel con filtros de fechas, estadísticas de asistencia y horas acumuladas de los alumnos.",
                Conexiones = "Historial cronológico detallado de inicios y cierres de sesión en las computadoras del laboratorio.",
                Escaneos = "Registro de lecturas de carnés universitarios y código de barras en el punto de acceso.",
                ResumenPrestamos = "Circulación diaria de la biblioteca física: libros prestados hoy, devoluciones registradas hoy, afluencia por hora, distribución por especialidad médica y tabla completa de movimientos del día con exportación a Excel.",
                EstadisticasPrestamos = "Métricas avanzadas de biblioteca filtrables por Semana, Mes o Año: flujo cronológico de préstamos vs devoluciones, top 10 libros más solicitados, distribución por especialidad médica y ranking de estudiantes lectores.",
                Prestamos = "Panel operativo de circulación: listado de préstamos activos y devueltos, marcado rápido de devoluciones en sala y sincronización automática con el sistema AbsysNet de la universidad.",
                Libros = "Catálogo general de libros médicos: títulos, autores, clasificación NLM/Dewey, número de registro, código de barras, portadas, estado y ubicación física en estantes.",
                Categorias = "Administración de categorías y especialidades médicas (ej. Anatomía, Patología, Farmacología, Cirugía) para clasificar el material bibliográfico.",
                MapaBiblioteca = "Visualización esquemática de la biblioteca física: estantes del 1 al 31, Caras A/B y pisos del 1 al 6 para ubicar rápidamente cualquier libro."
            };

            return Ok(new
            {
                FechaHora = now.ToString("yyyy-MM-dd HH:mm:ss"),
                FechaLegible = now.ToString("dddd, dd 'de' MMMM 'de' yyyy, hh:mm tt", new System.Globalization.CultureInfo("es-PE")),
                Biblioteca = new
                {
                    PrestamosHoy = prestamosHoy,
                    DevolucionesHoy = devolucionesHoy,
                    PrestamosActivos = prestamosActivos,
                    PrestamosVencidos = prestamosVencidos,
                    PorVencerHoy = porVencerHoy,
                    TasaCumplimientoHoy = $"{tasaATiempoHoy}%",
                    TotalCatalogo = totalCatalogo,
                    LibrosDisponibles = librosDisponibles,
                    LibrosPrestados = librosPrestadosActualmente,
                    TopLibrosHoy = topLibrosHoy,
                    UltimosMovimientos = ultimosMovimientos
                },
                Computo = new
                {
                    TotalEquipos = totalEquipos,
                    EquiposHabilitados = equiposHabilitados,
                    EquiposOcupados = equiposOcupados,
                    EquiposLibres = equiposLibres,
                    SesionesHoy = sesionesHoy,
                    AlumnosConectadosAhora = sesionesActivas.Count
                },
                Alumnos = new
                {
                    TotalRegistrados = totalAlumnos,
                    MultasPendientes = multasPendientes
                },
                SeccionesDelSistema = seccionesSistema
            });
        }

        // GET: api/Bot/BuscarLibro?titulo=harrison
        [HttpGet("BuscarLibro")]
        public async Task<IActionResult> BuscarLibro([FromQuery] string titulo)
        {
            if (string.IsNullOrWhiteSpace(titulo))
                return BadRequest(new { mensaje = "Debe especificar un término de búsqueda." });

            var term = titulo.Trim().ToLower();
            var resultados = await _context.Libros
                .Where(l => l.Titulo.ToLower().Contains(term) || l.Autor.ToLower().Contains(term) || l.CodigoBarras.ToLower().Contains(term))
                .Take(5)
                .Select(l => new
                {
                    l.LibroID,
                    l.Titulo,
                    l.Autor,
                    l.CodigoBarras,
                    l.NroClasificacion,
                    l.Categoria,
                    l.Estado,
                    Ubicacion = l.Estante.HasValue ? $"Estante {l.Estante} - Cara {l.Cara ?? "A"} - Piso {l.Piso ?? 1}" : "Sin ubicación asignada"
                })
                .ToListAsync();

            return Ok(resultados);
        }

        // GET: api/Bot/ConsultarAlumno?codigo=20211045
        [HttpGet("ConsultarAlumno")]
        public async Task<IActionResult> ConsultarAlumno([FromQuery] string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return BadRequest(new { mensaje = "Debe especificar un código universitario o DNI." });

            var term = codigo.Trim().ToLower();
            var alumno = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.CodigoUniversitario.ToLower() == term || a.DNI.ToLower() == term);

            if (alumno == null)
            {
                return NotFound(new { mensaje = $"No se encontró ningún estudiante con el código o DNI '{codigo}'." });
            }

            var now = TimeHelper.GetPeruTime();
            var sesionActiva = await _context.Sesiones
                .Include(s => s.Equipo)
                .FirstOrDefaultAsync(s => s.AlumnoID == alumno.AlumnoID && s.HoraFin == null);

            var prestamosActivos = await _context.Prestamos
                .Include(p => p.Libro)
                .Where(p => p.AlumnoID == alumno.AlumnoID && (p.Estado == "Activo" || p.Estado == "Renovado"))
                .Select(p => new
                {
                    p.PrestamoID,
                    Libro = p.Libro != null ? p.Libro.Titulo : "Sin título",
                    p.FechaPrestamo,
                    p.FechaDevolucion,
                    EsVencido = p.FechaDevolucion < now
                })
                .ToListAsync();

            var multasPendientes = await _context.Multas
                .Where(m => m.AlumnoID == alumno.AlumnoID && m.Estado == "Pendiente")
                .Select(m => new
                {
                    m.MultaID,
                    m.Monto,
                    m.FechaEmision
                })
                .ToListAsync();

            return Ok(new
            {
                alumno.AlumnoID,
                alumno.CodigoUniversitario,
                NombreCompleto = $"{alumno.Nombres} {alumno.ApellidoPaterno} {alumno.ApellidoMaterno}".Trim(),
                alumno.Carrera,
                EstadoHabilitado = alumno.Estado,
                ComputadoraActual = sesionActiva?.Equipo != null ? sesionActiva.Equipo.NombreRed : "Ninguna (No está en sala)",
                PrestamosActivos = prestamosActivos,
                MultasPendientes = multasPendientes
            });
        }

        // POST: api/Bot/Chat
        // Responde preguntas del administrador utilizando datos reales de la BD y conocimiento del sistema
        [HttpPost("Chat")]
        public async Task<IActionResult> Chat([FromBody] BotChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Mensaje))
            {
                return BadRequest(new { reply = "Por favor, escribe una consulta válida." });
            }

            var query = request.Mensaje.Trim().ToLower();
            var now = TimeHelper.GetPeruTime();
            var today = now.Date;

            // Saludos y presentación
            if (query == "hola" || query.StartsWith("hola ") || query.Contains("buenos dias") || query.Contains("buenos días") || query.Contains("buenas tardes") || query.Contains("buenas noches") || query.Contains("quien eres") || query.Contains("quién eres"))
            {
                return Ok(new
                {
                    reply = "¡Hola! 👋 Soy **BiblioBot**, el asistente inteligente de la Biblioteca Virtual y Laboratorio de Cómputo (BVE Medicina - URP).\n\n" +
                            "Estoy conectado en tiempo real a la base de datos del sistema para ayudarte con:\n" +
                            "• **Consultas en vivo:** *\"¿Cuántos libros se prestaron hoy?\"*, *\"¿Cuántas computadoras están libres?\"*\n" +
                            "• **Explicación de módulos:** *\"¿Para qué sirve la sección de reportes?\"*, *\"¿Qué hace el mapa de estaciones?\"*\n" +
                            "• **Búsqueda bibliográfica:** *\"¿Dónde encuentro el libro Harrison?\"*\n" +
                            "• **Consultas de alumnos:** *\"Consultar alumno 20171204\"*\n\n" +
                            "¿Qué te gustaría saber hoy?"
                });
            }

            // 1. Preguntas sobre préstamos de hoy
            if ((query.Contains("cuanto") || query.Contains("cuántos") || query.Contains("libros") || query.Contains("prestamo") || query.Contains("préstamo")) && (query.Contains("hoy") || query.Contains("prestado") || query.Contains("prestados")))
            {
                var prestamosHoy = await _context.Prestamos.CountAsync(p => p.FechaPrestamo.Date == today);
                var devolucionesHoy = await _context.Prestamos.CountAsync(p => p.FechaEntregado.HasValue && p.FechaEntregado.Value.Date == today);
                var prestamosActivos = await _context.Prestamos.CountAsync(p => p.Estado == "Activo" || p.Estado == "Renovado");
                var prestamosVencidos = await _context.Prestamos.CountAsync(p => (p.Estado == "Activo" || p.Estado == "Renovado") && p.FechaDevolucion < now);

                return Ok(new
                {
                    reply = $"📚 **Circulación de Libros Hoy ({now:dd/MM/yyyy}):**\n\n" +
                            $"• **Préstamos registrados hoy:** **{prestamosHoy}** libro(s)\n" +
                            $"• **Devoluciones procesadas hoy:** **{devolucionesHoy}** libro(s)\n" +
                            $"• **Préstamos actualmente activos:** **{prestamosActivos}** en sala o domicilio\n" +
                            $"• **Préstamos vencidos:** **{prestamosVencidos}** con retraso"
                });
            }

            // 2. Preguntas sobre computadoras / laboratorio
            if (query.Contains("computadora") || query.Contains("computadoras") || query.Contains("pc") || query.Contains("pcs") || query.Contains("estacion") || query.Contains("estaciones") || query.Contains("libre") || query.Contains("libres") || query.Contains("ocupad"))
            {
                var totalEquipos = await _context.Equipos.CountAsync();
                var equiposHabilitados = await _context.Equipos.CountAsync(e => e.Estado);
                var sesionesActivas = await _context.Sesiones.Where(s => s.HoraFin == null).ToListAsync();
                var equiposOcupados = sesionesActivas.Select(s => s.EquipoID).Distinct().Count();
                var equiposLibres = Math.Max(0, equiposHabilitados - equiposOcupados);

                return Ok(new
                {
                    reply = $"🖥️ **Estado del Laboratorio de Cómputo:**\n\n" +
                            $"• **Computadoras Libres:** **{equiposLibres}** disponibles\n" +
                            $"• **Computadoras Ocupadas:** **{equiposOcupados}** en uso\n" +
                            $"• **Total Operativas:** **{equiposHabilitados}** (de {totalEquipos} registradas)\n" +
                            $"• **Alumnos en sala ahora mismo:** **{sesionesActivas.Count}** estudiante(s)"
                });
            }

            // 3. Preguntas sobre la sección de Reportes
            if (query.Contains("reporte") || query.Contains("reportes"))
            {
                return Ok(new
                {
                    reply = "📊 **Sección de Reportes:**\n\n" +
                            "La sección de **Reportes** es el módulo oficial para generar, auditar y exportar informes ejecutivos en formato Excel (.xlsx):\n\n" +
                            "• **Asistencia General:** Historial completo de accesos de alumnos con fecha, hora de entrada y hora de salida.\n" +
                            "• **Horas de Cómputo Acumuladas:** Control del tiempo consumido por cada estudiante respecto a su límite diario.\n" +
                            "• **Filtros por Período:** Permite seleccionar rangos por día, mes o año.\n" +
                            "• **Descarga en Excel:** Genera hojas de cálculo oficiales para auditoría o informes a Decanato."
                });
            }

            // 4. Preguntas sobre Resumen Diario
            if (query.Contains("resumen diario") || (query.Contains("resumen") && !query.Contains("prestamo") && !query.Contains("préstamo")))
            {
                return Ok(new
                {
                    reply = "📋 **Sección Resumen Diario (Laboratorio):**\n\n" +
                            "Es el panel de monitoreo en tiempo real del Laboratorio de Cómputo para la jornada actual:\n\n" +
                            "• Monitoreo de sesiones activas en sala y tiempo promedio de uso.\n" +
                            "• Porcentaje de ocupación instantánea de las computadoras.\n" +
                            "• Gráfica de afluencia por hora (horas punta de mayor concurrencia).\n" +
                            "• Selector de fecha para consultar cualquier día anterior."
                });
            }

            // 5. Preguntas sobre Resumen Diario de Préstamos
            if (query.Contains("resumen") && (query.Contains("prestamo") || query.Contains("préstamo")))
            {
                return Ok(new
                {
                    reply = "📖 **Sección Resumen Diario de Préstamos:**\n\n" +
                            "Brinda la analítica en tiempo real de la biblioteca para el día seleccionado:\n\n" +
                            "• **KPIs del Día:** Préstamos de hoy, devoluciones de hoy, préstamos activos y tasa de cumplimiento.\n" +
                            "• **Afluencia por Hora:** Gráfica comparativa de salidas vs retornos a lo largo de la jornada.\n" +
                            "• **Distribución por Especialidad:** Gráfico dona con las categorías médicas más solicitadas.\n" +
                            "• **Tabla de Movimientos:** Detalle de cada libro prestado o devuelto con buscador y exportación a Excel."
                });
            }

            // 6. Preguntas sobre Estadísticas de Préstamos
            if (query.Contains("estadistica") && (query.Contains("prestamo") || query.Contains("préstamo") || query.Contains("libro")))
            {
                return Ok(new
                {
                    reply = "📈 **Sección Estadísticas de Préstamos:**\n\n" +
                            "Analítica métrica histórica de la biblioteca sincronizada con AbsysNet:\n\n" +
                            "• **Filtros por Período:** Puedes analizar por **Semana**, **Mes** o **Año**.\n" +
                            "• **Flujo Temporal:** Curva cronológica de préstamos vs devoluciones.\n" +
                            "• **Top 10 Libros:** Títulos con mayor demanda en el período.\n" +
                            "• **Distribución Médica:** Especialidades de mayor circulación.\n" +
                            "• **Ranking de Estudiantes Lectores:** Alumnos con más préstamos registrados."
                });
            }

            // 7. Preguntas sobre Estadísticas de Equipos
            if (query.Contains("estadistica") && (query.Contains("equipo") || query.Contains("computadora") || query.Contains("lab")))
            {
                return Ok(new
                {
                    reply = "💻 **Sección Estadísticas de Equipos:**\n\n" +
                            "Muestra el histórico de demanda del laboratorio de cómputo:\n\n" +
                            "• Horas totales de uso acumuladas por mes o año.\n" +
                            "• Estudiantes únicos atendidos en el período.\n" +
                            "• Tendencia semanal y mensual de horas de utilización por estación."
                });
            }

            // 8. En Línea / Sesiones Activas
            if (query.Contains("en linea") || query.Contains("en línea") || query.Contains("conectado") || query.Contains("conectados") || query.Contains("sesiones activas"))
            {
                var activas = await _context.Sesiones
                    .Include(s => s.Equipo)
                    .Include(s => s.Alumno)
                    .Where(s => s.HoraFin == null)
                    .ToListAsync();

                if (!activas.Any())
                {
                    return Ok(new
                    {
                        reply = "🟢 **Sección En Línea:**\n\nActualmente **no hay sesiones activas** en el laboratorio de cómputo. Todas las computadoras habilitadas están desocupadas."
                    });
                }

                var resumenAlumnos = string.Join("\n", activas.Take(5).Select(s => $"• **{s.Equipo?.NombreRed ?? "PC"}**: {s.Alumno?.Nombres} {s.Alumno?.ApellidoPaterno} (desde {s.HoraInicio:hh\\:mm})"));
                return Ok(new
                {
                    reply = $"🟢 **Sección En Línea ({activas.Count} usuarios conectados):**\n\n" +
                            $"{resumenAlumnos}\n\n" +
                            (activas.Count > 5 ? $"*... y {activas.Count - 5} alumnos más.*" : "")
                });
            }

            // 9. Mapa de Estaciones
            if (query.Contains("mapa") && (query.Contains("estacion") || query.Contains("computadora") || query.Contains("red") || query.Contains("lab")))
            {
                return Ok(new
                {
                    reply = "🗺️ **Sección Mapa de Estaciones:**\n\n" +
                            "Representación visual en cuadrícula de la distribución física de las 67 computadoras del laboratorio:\n\n" +
                            "• Cada cuadro representa una PC identificada con su nombre de red.\n" +
                            "• Colores en vivo: Verde (Libre), Rojo (Ocupada), Gris (Fuera de servicio).\n" +
                            "• Al hacer clic en una estación ocupada, muestra qué alumno la está usando y desde qué hora."
                });
            }

            // 10. Mapa de Biblioteca
            if (query.Contains("mapa") && (query.Contains("biblio") || query.Contains("libro") || query.Contains("estante") || query.Contains("piso")))
            {
                return Ok(new
                {
                    reply = "📚 **Sección Mapa de la Biblioteca:**\n\n" +
                            "Esquema interactivo de la distribución física de las estanterías de libros médicos:\n\n" +
                            "• Estanterías numeradas del **Estante 1 al 31**.\n" +
                            "• División por caras (**Cara A / Cara B**) y niveles de repisa (**Pisos 1 al 6**).\n" +
                            "• Permite localizar físicamente cualquier libro en sala al instante."
                });
            }

            // 11. AbsysNet
            if (query.Contains("absys") || query.Contains("absysnet") || query.Contains("sincroniz"))
            {
                return Ok(new
                {
                    reply = "🔄 **Integración con AbsysNet:**\n\n" +
                            "El sistema cuenta con un conector automático vía UserScript (`absysnet-sync.user.js`) y API REST:\n\n" +
                            "• Cada vez que se registra un préstamo o devolución en AbsysNet, se sincroniza en milisegundos con Control-Laboratorio.\n" +
                            "• Registra el alumno, libro, código de barras, fecha de préstamo y fecha límite oficial.\n" +
                            "• Permite control unificado de circulación y métricas automáticas."
                });
            }

            // 12. Categorías / Especialidades
            if (query.Contains("categoria") || query.Contains("categoría") || query.Contains("especialidad"))
            {
                var totalCategorias = await _context.Categorias.CountAsync();
                return Ok(new
                {
                    reply = $"🏷️ **Sección Categorías Médicas:**\n\n" +
                            $"Administra las especialidades y áreas de conocimiento del acervo bibliográfico (actualmente **{totalCategorias} categorías registradas**):\n\n" +
                            $"• Ejemplos: *Anatomía, Patología, Farmacología, Cirugía, Cardiología, Pediatría*.\n" +
                            $"• Facilita el filtrado en el catálogo y genera los gráficos de distribución por especialidad médica."
                });
            }

            // 13. Búsqueda directa de Libro (si contiene 'donde esta', 'buscar libro', o títulos conocidos como 'harrison', 'robbins', 'anatomia', etc.)
            if (query.Contains("donde esta") || query.Contains("dónde está") || query.Contains("donde encuentro") || query.Contains("dónde encuentro") || query.Contains("harrison") || query.Contains("robbins") || query.Contains("guyton") || query.Contains("netter") || query.Contains("buscar libro"))
            {
                string searchKeyword = query
                    .Replace("donde esta el libro", "")
                    .Replace("dónde está el libro", "")
                    .Replace("donde encuentro el libro", "")
                    .Replace("dónde encuentro el libro", "")
                    .Replace("donde esta", "")
                    .Replace("dónde está", "")
                    .Replace("buscar libro", "")
                    .Replace("libro", "")
                    .Trim();

                if (string.IsNullOrEmpty(searchKeyword)) searchKeyword = "a";

                var librosEncontrados = await _context.Libros
                    .Where(l => l.Titulo.ToLower().Contains(searchKeyword) || l.Autor.ToLower().Contains(searchKeyword))
                    .Take(3)
                    .ToListAsync();

                if (librosEncontrados.Any())
                {
                    var items = string.Join("\n\n", librosEncontrados.Select(l =>
                        $"📖 **{l.Titulo}**\n" +
                        $"• **Autor:** {l.Autor ?? "No especificado"}\n" +
                        $"• **Clasificación:** {l.NroClasificacion ?? "S/C"} | **Estado:** {l.Estado}\n" +
                        $"• **Ubicación:** {(l.Estante.HasValue ? $"Estante {l.Estante} - Cara {l.Cara ?? "A"} - Piso {l.Piso ?? 1}" : "Sin ubicación en estante")}"
                    ));

                    return Ok(new
                    {
                        reply = $"🔍 **Libros Encontrados:**\n\n{items}"
                    });
                }
            }

            // 14. Catálogo de Libros (general)
            if (query.Contains("libro") || query.Contains("catalogo") || query.Contains("catálogo"))
            {
                var totalLibros = await _context.Libros.CountAsync();
                var disponibles = await _context.Libros.CountAsync(l => l.Estado == "Disponible");

                return Ok(new
                {
                    reply = $"📚 **Catálogo de Libros:**\n\n" +
                            $"Permite registrar y administrar el acervo bibliográfico físico de Medicina:\n\n" +
                            $"• Actualmente hay **{totalLibros} libros registrados** ({disponibles} disponibles para préstamo).\n" +
                            $"• Registra código de barras, clasificación NLM/Dewey, autor, editorial y año.\n" +
                            $"• Ubicación física precisa: Estantes del 1 al 31, Cara A/B y Pisos del 1 al 6.\n" +
                            $"• Importación masiva desde archivos Excel (.xlsx)."
                });
            }

            // 15. Participantes / Alumnos
            if (query.Contains("participante") || query.Contains("participantes") || query.Contains("alumno") || query.Contains("alumnos") || query.Contains("estudiante"))
            {
                // Verificar si hay un código en el texto
                var digitos = new string(query.Where(char.IsDigit).ToArray());
                if (digitos.Length >= 6)
                {
                    var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == digitos || a.DNI == digitos);
                    if (alumno != null)
                    {
                        var sesion = await _context.Sesiones.Include(s => s.Equipo).FirstOrDefaultAsync(s => s.AlumnoID == alumno.AlumnoID && s.HoraFin == null);
                        var prestamos = await _context.Prestamos.Include(p => p.Libro).Where(p => p.AlumnoID == alumno.AlumnoID && (p.Estado == "Activo" || p.Estado == "Renovado")).ToListAsync();
                        var multas = await _context.Multas.Where(m => m.AlumnoID == alumno.AlumnoID && m.Estado == "Pendiente").ToListAsync();

                        return Ok(new
                        {
                            reply = $"👨‍🎓 **Ficha del Estudiante:**\n\n" +
                                    $"• **Nombre:** {alumno.Nombres} {alumno.ApellidoPaterno} {alumno.ApellidoMaterno}\n" +
                                    $"• **Código:** {alumno.CodigoUniversitario} | **Carrera:** {alumno.Carrera}\n" +
                                    $"• **Estado de Acceso:** {(alumno.Estado ? "✅ Habilitado" : "❌ Bloqueado")}\n" +
                                    $"• **Sesión de Cómputo Actual:** {(sesion?.Equipo != null ? $"🟢 Conectado en {sesion.Equipo.NombreRed}" : "⚪ No está en sala")}\n" +
                                    $"• **Préstamos de Libros Activos:** {prestamos.Count} libro(s)\n" +
                                    $"• **Multas Pendientes:** {multas.Count}"
                        });
                    }
                }

                var totalAlumnos = await _context.Alumnos.CountAsync();
                var multasPendientes = await _context.Multas.CountAsync(m => m.Estado == "Pendiente");

                return Ok(new
                {
                    reply = $"👨‍🎓 **Sección Participantes (Estudiantes):**\n\n" +
                            $"Padrón oficial de estudiantes de la Facultad de Medicina Humana:\n\n" +
                            $"- Total de alumnos registrados: **{totalAlumnos}**\n" +
                            $"- Multas o sanciones pendientes: **{multasPendientes}**\n" +
                            $"• Control automático del límite de tiempo diario de cómputo.\n" +
                            $"• Historial de asistencias, bloqueos y validación de carné universitario."
                });
            }

            // Respuesta por defecto con sugerencias
            return Ok(new
            {
                reply = $"Entendido. He recibido tu consulta: *\"{request.Mensaje}\"*.\n\nPuedes preguntarme sobre:\n" +
                        $"• **Métricas en vivo:** *\"¿Cuántos libros se prestaron hoy?\"*, *\"¿Cuántas PCs están libres?\"*\n" +
                        $"• **Módulos del sistema:** *\"¿Para qué sirve la sección de reportes?\"*, *\"¿Qué hace el mapa de estaciones?\"*\n" +
                        $"• **Búsqueda de material:** *\"¿Dónde está el libro Harrison?\"*"
            });
        }
    }

    public class BotChatRequest
    {
        public string Mensaje { get; set; } = string.Empty;
        public string? Remitente { get; set; }
    }
}
