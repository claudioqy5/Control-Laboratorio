using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.DTOs;
using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/admin/objetos-perdidos")]
    [ApiController]
    [Authorize]
    public class ObjetosPerdidosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ObjetosPerdidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] byte? categoria,
            [FromQuery] string? tab = "todos")
        {
            var ahora = DateTime.UtcNow;
            var fechaLimite = ahora.AddDays(-30);

            var query = _context.ObjetosPerdidos.AsNoTracking().AsQueryable();

            var selectedTab = (tab ?? "todos").Trim().ToLowerInvariant();

            switch (selectedTab)
            {
                case "custodia":
                    query = query.Where(o => o.EstadoObjeto == EstadoObjeto.EnCustodia && o.FechaHoraHallazgo >= fechaLimite);
                    break;
                case "devueltos":
                    query = query.Where(o => o.EstadoObjeto == EstadoObjeto.Devuelto);
                    break;
                case "vencidos":
                    query = query.Where(o => o.EstadoObjeto == EstadoObjeto.EnCustodia && o.FechaHoraHallazgo < fechaLimite);
                    break;
                case "bajas":
                    query = query.Where(o => o.EstadoObjeto == EstadoObjeto.Dispuesto);
                    break;
                case "todos":
                default:
                    break;
            }

            if (categoria.HasValue && categoria.Value > 0)
            {
                var catEnum = (CategoriaObjeto)categoria.Value;
                query = query.Where(o => o.Categoria == catEnum);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(o =>
                    o.Titulo.Contains(term) ||
                    o.Descripcion.Contains(term) ||
                    o.LugarHallazgo.Contains(term) ||
                    (o.CodigoURPReceptor != null && o.CodigoURPReceptor.Contains(term)) ||
                    (o.DocumentoReceptor != null && o.DocumentoReceptor.Contains(term)));
            }

            query = query.OrderByDescending(o => o.FechaHoraHallazgo);

            var items = await query.ToListAsync();

            var resultado = items.Select(item => MapToDto(item, ahora)).ToList();

            return Ok(resultado);
        }

        [HttpGet("kpis")]
        public async Task<ActionResult<ObjetosPerdidosKpisDto>> GetKpis()
        {
            var fechaLimite = DateTime.UtcNow.AddDays(-30);

            var totalCustodia = await _context.ObjetosPerdidos
                .CountAsync(o => o.EstadoObjeto == EstadoObjeto.EnCustodia && o.FechaHoraHallazgo >= fechaLimite);

            var totalDevueltos = await _context.ObjetosPerdidos
                .CountAsync(o => o.EstadoObjeto == EstadoObjeto.Devuelto);

            var totalVencidos = await _context.ObjetosPerdidos
                .CountAsync(o => o.EstadoObjeto == EstadoObjeto.EnCustodia && o.FechaHoraHallazgo < fechaLimite);

            var totalBajas = await _context.ObjetosPerdidos
                .CountAsync(o => o.EstadoObjeto == EstadoObjeto.Dispuesto);

            var totalRegistrados = await _context.ObjetosPerdidos
                .CountAsync();

            return Ok(new ObjetosPerdidosKpisDto
            {
                TotalCustodia = totalCustodia,
                TotalDevueltos = totalDevueltos,
                TotalVencidos = totalVencidos,
                TotalBajas = totalBajas,
                TotalRegistrados = totalRegistrados
            });
        }

        [HttpGet("verificar-alumno/{codigo}")]
        public async Task<IActionResult> VerificarAlumno(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return BadRequest(new { existe = false, message = "Código o documento no proporcionado." });
            }

            var cleanCodigo = codigo.Trim();

            var alumno = await _context.Alumnos
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.CodigoUniversitario == cleanCodigo || a.DNI == cleanCodigo);

            if (alumno == null)
            {
                return Ok(new { existe = false, message = "Estudiante no figura en el sistema" });
            }

            return Ok(new
            {
                existe = true,
                alumnoId = alumno.AlumnoID,
                codigoUniversitario = alumno.CodigoUniversitario,
                dni = alumno.DNI,
                nombres = $"{alumno.Nombres} {alumno.ApellidoPaterno} {alumno.ApellidoMaterno}".Trim(),
                carrera = alumno.Carrera,
                correoInstitucional = alumno.CorreoInstitucional
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.ObjetosPerdidos
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.IdObjeto == id);

            if (item == null)
            {
                return NotFound(new { message = "Objeto perdido no encontrado." });
            }

            return Ok(MapToDto(item, DateTime.UtcNow));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RegistrarObjetoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!Enum.IsDefined(typeof(CategoriaObjeto), dto.Categoria))
            {
                return BadRequest(new { message = "Categoría no válida." });
            }

            var usuario = await _context.UsuariosAdmin.FindAsync(dto.IdUsuarioHallazgo);
            if (usuario == null)
            {
                return BadRequest(new { message = "El personal de registro especificado no existe." });
            }

            var objeto = new ObjetoPerdido
            {
                Titulo = dto.Titulo.Trim(),
                Categoria = dto.Categoria,
                Descripcion = dto.Descripcion.Trim(),
                LugarHallazgo = dto.LugarHallazgo.Trim(),
                FechaHoraHallazgo = dto.FechaHoraHallazgo,
                EstadoObjeto = EstadoObjeto.EnCustodia,
                IdUsuarioHallazgo = usuario.IdUsuarioAdmin,
                NombrePersonalHallazgo = $"{usuario.Username} • {usuario.Nombres}",
                FechaCreacion = DateTime.UtcNow
            };

            _context.ObjetosPerdidos.Add(objeto);
            await _context.SaveChangesAsync();

            var resultDto = MapToDto(objeto, DateTime.UtcNow);

            return CreatedAtAction(nameof(GetById), new { id = objeto.IdObjeto }, resultDto);
        }

        [HttpPost("{id:int}/devolver")]
        public async Task<IActionResult> Devolver(int id, [FromBody] DevolverObjetoDto dto)
        {
            var objeto = await _context.ObjetosPerdidos.FindAsync(id);
            if (objeto == null)
            {
                return NotFound(new { message = "Objeto perdido no encontrado." });
            }

            if (objeto.EstadoObjeto != EstadoObjeto.EnCustodia)
            {
                return BadRequest(new { message = "Solo se pueden devolver objetos que se encuentren en custodia." });
            }

            if (string.IsNullOrWhiteSpace(dto.CodigoUniversitario))
            {
                return BadRequest(new { message = "El código universitario es obligatorio." });
            }

            var codigo = dto.CodigoUniversitario.Trim();
            var alumno = await _context.Alumnos.FirstOrDefaultAsync(a => a.CodigoUniversitario == codigo);
            if (alumno == null)
            {
                return NotFound(new { message = "El alumno no figura en el padrón institucional." });
            }

            var usuario = await _context.UsuariosAdmin.FindAsync(dto.IdUsuarioEntrega);
            if (usuario == null)
            {
                return BadRequest(new { message = "Personal de entrega no válido." });
            }

            objeto.EstadoObjeto = EstadoObjeto.Devuelto;
            objeto.FechaHoraDevolucion = DateTime.UtcNow;
            objeto.AlumnoID = alumno.AlumnoID;
            objeto.DocumentoReceptor = alumno.DNI;
            objeto.CodigoURPReceptor = alumno.CodigoUniversitario;
            objeto.IdUsuarioEntrega = usuario.IdUsuarioAdmin;
            objeto.NombrePersonalEntrega = $"{usuario.Username} • {usuario.Nombres}";
            objeto.ObservacionesEntrega = dto.Observaciones?.Trim();

            await _context.SaveChangesAsync();

            var objetoDto = MapToDto(objeto, DateTime.UtcNow);

            return Ok(new
            {
                message = "Objeto devuelto exitosamente.",
                objeto = objetoDto
            });
        }

        [HttpPost("{id:int}/dar-de-baja")]
        public async Task<IActionResult> DarDeBaja(int id)
        {
            var objeto = await _context.ObjetosPerdidos.FindAsync(id);
            if (objeto == null)
            {
                return NotFound(new { message = "Objeto perdido no encontrado." });
            }

            if (objeto.EstadoObjeto == EstadoObjeto.Devuelto)
            {
                return BadRequest(new { message = "No se puede dar de baja un objeto que ya fue devuelto." });
            }

            objeto.EstadoObjeto = EstadoObjeto.Dispuesto;
            objeto.FechaBaja = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var objetoDto = MapToDto(objeto, DateTime.UtcNow);

            return Ok(new
            {
                message = "Objeto dado de baja exitosamente.",
                objeto = objetoDto
            });
        }

        private static ObjetoPerdidoDto MapToDto(ObjetoPerdido item, DateTime ahora)
        {
            var diasTranscurridos = (ahora - item.FechaHoraHallazgo).TotalDays;
            var diasRestantes = Math.Max(0, (int)Math.Ceiling(30 - diasTranscurridos));
            var esVencido = item.EstadoObjeto == EstadoObjeto.EnCustodia && diasTranscurridos > 30;

            return new ObjetoPerdidoDto
            {
                IdObjeto = item.IdObjeto,
                Titulo = item.Titulo,
                Categoria = item.Categoria,
                CategoriaNombre = ObtenerCategoriaNombre(item.Categoria),
                Descripcion = item.Descripcion,
                LugarHallazgo = item.LugarHallazgo,
                FechaHoraHallazgo = item.FechaHoraHallazgo,
                EstadoObjeto = item.EstadoObjeto,
                EstadoNombre = ObtenerEstadoNombre(item.EstadoObjeto, esVencido),
                IdUsuarioHallazgo = item.IdUsuarioHallazgo,
                NombrePersonalHallazgo = item.NombrePersonalHallazgo,
                FechaHoraDevolucion = item.FechaHoraDevolucion,
                AlumnoID = item.AlumnoID,
                DocumentoReceptor = item.DocumentoReceptor,
                CodigoURPReceptor = item.CodigoURPReceptor,
                IdUsuarioEntrega = item.IdUsuarioEntrega,
                NombrePersonalEntrega = item.NombrePersonalEntrega,
                ObservacionesEntrega = item.ObservacionesEntrega,
                FechaBaja = item.FechaBaja,
                DiasRestantes = diasRestantes,
                EsVencido = esVencido
            };
        }

        private static string ObtenerCategoriaNombre(CategoriaObjeto categoria) => categoria switch
        {
            CategoriaObjeto.Electronica => "Electrónica",
            CategoriaObjeto.Documentos => "Documentos",
            CategoriaObjeto.Estudio => "Estudio",
            CategoriaObjeto.Prendas => "Prendas",
            CategoriaObjeto.Otros => "Otros",
            _ => categoria.ToString()
        };

        private static string ObtenerEstadoNombre(EstadoObjeto estado, bool esVencido) => estado switch
        {
            EstadoObjeto.Devuelto => "Devuelto",
            EstadoObjeto.Dispuesto => "Dado de Baja",
            EstadoObjeto.EnCustodia => esVencido ? "Vencido" : "En Custodia",
            _ => estado.ToString()
        };
    }
}
