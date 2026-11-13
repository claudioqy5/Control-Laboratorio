using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.DTOs;
using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.Controllers
{
    [ApiController]
    [Route("api/admin/resources")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminResourcesController : ControllerBase
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private const int MaxDestacadosCount = 6;
        private static readonly string[] AllowedExtensions = [".webp", ".jpg", ".jpeg", ".png"];

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminResourcesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] bool? esSuscripcion,
            [FromQuery] bool? activo,
            [FromQuery] bool? destacadosOnly)
        {
            var query = _context.BasesDatosMedicas
                .Include(r => r.RecursosCursos)
                .ThenInclude(rc => rc.Curso)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(r => r.NombreRecurso.ToLower().Contains(term)
                    || r.DescripcionClinica.ToLower().Contains(term)
                    || r.RecursosCursos.Any(rc => rc.Curso.NombreCurso.ToLower().Contains(term)));
            }

            if (esSuscripcion.HasValue)
            {
                query = query.Where(r => r.EsSuscripcion == esSuscripcion.Value);
            }

            if (activo.HasValue)
            {
                query = query.Where(r => r.EstadoActivo == activo.Value);
            }

            if (destacadosOnly == true)
            {
                query = query.Where(r => r.MostrarEnHexagonos);
            }

            var resources = await query
                .OrderByDescending(r => r.MostrarEnHexagonos)
                .ThenBy(r => r.NombreRecurso)
                .Select(r => MapToAdminDto(r))
                .ToListAsync();

            return Ok(resources);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var resource = await _context.BasesDatosMedicas
                .Include(r => r.RecursosCursos)
                .ThenInclude(rc => rc.Curso)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdBaseDatos == id);

            if (resource == null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            return Ok(MapToAdminDto(resource));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateResourceFormDto request)
        {
            if (request.Image == null || request.Image.Length == 0)
            {
                return BadRequest(new { message = "Se requiere una imagen de logotipo válida." });
            }

            if (request.Image.Length > MaxImageSizeBytes)
            {
                return BadRequest(new { message = "El tamaño de la imagen no debe superar los 5 MB." });
            }

            var extension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Formato no permitido. Solo se aceptan extensiones .webp, .jpg, .jpeg y .png." });
            }

            if (string.IsNullOrWhiteSpace(request.NombreRecurso))
            {
                return BadRequest(new { message = "El nombre del recurso es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(request.DescripcionClinica))
            {
                return BadRequest(new { message = "La descripción clínica es obligatoria." });
            }

            if (request.MostrarEnHexagonos)
            {
                var featuredCount = await _context.BasesDatosMedicas.CountAsync(r => r.MostrarEnHexagonos && r.EstadoActivo);
                if (featuredCount >= MaxDestacadosCount)
                {
                    return BadRequest(new { message = $"Límite alcanzado: Ya existen {MaxDestacadosCount} bases de datos destacadas en la cuadrícula de inicio. Desactive otra antes de destacar esta." });
                }
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var targetFolder = Path.Combine(rootPath, "recursos");

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            var filePath = Path.Combine(targetFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            var entity = new BaseDatosMedica
            {
                NombreRecurso = request.NombreRecurso.Trim(),
                LogotipoUrl = $"/api/static/recursos/{fileName}",
                DescripcionClinica = request.DescripcionClinica.Trim(),
                EsSuscripcion = request.EsSuscripcion,
                TieneAppMovil = request.TieneAppMovil,
                UrlExterno = request.EsSuscripcion ? null : (string.IsNullOrWhiteSpace(request.UrlExterno) ? null : request.UrlExterno.Trim()),
                EstadoActivo = true,
                MostrarEnHexagonos = request.MostrarEnHexagonos,
                YoutubeVideoId = CleanYoutubeVideoId(request.YoutubeVideoId)
            };

            _context.BasesDatosMedicas.Add(entity);
            await _context.SaveChangesAsync();

            var courseIds = ParseCursoIds(request.CursoIds, request.CursoIdsRaw);
            if (courseIds.Count > 0)
            {
                var validCourseIds = await _context.CursosMedicina
                    .Where(c => courseIds.Contains(c.IdCurso))
                    .Select(c => c.IdCurso)
                    .ToListAsync();

                foreach (var courseId in validCourseIds)
                {
                    _context.RecursosCursos.Add(new RecursoCurso
                    {
                        IdBaseDatos = entity.IdBaseDatos,
                        IdCurso = courseId
                    });
                }
                await _context.SaveChangesAsync();
            }

            var result = await _context.BasesDatosMedicas
                .Include(r => r.RecursosCursos)
                .ThenInclude(rc => rc.Curso)
                .AsNoTracking()
                .FirstAsync(r => r.IdBaseDatos == entity.IdBaseDatos);

            return CreatedAtAction(nameof(GetById), new { id = entity.IdBaseDatos }, MapToAdminDto(result));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateResourceFormDto request)
        {
            var entity = await _context.BasesDatosMedicas
                .Include(r => r.RecursosCursos)
                .FirstOrDefaultAsync(r => r.IdBaseDatos == id);

            if (entity == null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            if (string.IsNullOrWhiteSpace(request.NombreRecurso))
            {
                return BadRequest(new { message = "El nombre del recurso es obligatorio." });
            }

            if (string.IsNullOrWhiteSpace(request.DescripcionClinica))
            {
                return BadRequest(new { message = "La descripción clínica es obligatoria." });
            }

            if (request.MostrarEnHexagonos && !entity.MostrarEnHexagonos)
            {
                var featuredCount = await _context.BasesDatosMedicas
                    .CountAsync(r => r.MostrarEnHexagonos && r.EstadoActivo && r.IdBaseDatos != id);

                if (featuredCount >= MaxDestacadosCount)
                {
                    return BadRequest(new { message = $"Límite alcanzado: Ya existen {MaxDestacadosCount} bases de datos destacadas en la cuadrícula de inicio. Desactive otra antes de destacar esta." });
                }
            }

            if (request.Image != null && request.Image.Length > 0)
            {
                if (request.Image.Length > MaxImageSizeBytes)
                {
                    return BadRequest(new { message = "El tamaño de la imagen no debe superar los 5 MB." });
                }

                var extension = Path.GetExtension(request.Image.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    return BadRequest(new { message = "Formato no permitido. Solo se aceptan extensiones .webp, .jpg, .jpeg y .png." });
                }

                var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var targetFolder = Path.Combine(rootPath, "recursos");

                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                var oldFileName = Path.GetFileName(entity.LogotipoUrl);
                if (!string.IsNullOrEmpty(oldFileName))
                {
                    var oldFilePath = Path.Combine(targetFolder, oldFileName);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                var newFileName = $"{Guid.NewGuid()}{extension}";
                var newFilePath = Path.Combine(targetFolder, newFileName);
                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await request.Image.CopyToAsync(stream);
                }

                entity.LogotipoUrl = $"/api/static/recursos/{newFileName}";
            }

            entity.NombreRecurso = request.NombreRecurso.Trim();
            entity.DescripcionClinica = request.DescripcionClinica.Trim();
            entity.EsSuscripcion = request.EsSuscripcion;
            entity.TieneAppMovil = request.TieneAppMovil;
            entity.UrlExterno = request.EsSuscripcion ? null : (string.IsNullOrWhiteSpace(request.UrlExterno) ? null : request.UrlExterno.Trim());
            entity.EstadoActivo = request.EstadoActivo;
            entity.MostrarEnHexagonos = request.EstadoActivo && request.MostrarEnHexagonos;
            entity.YoutubeVideoId = CleanYoutubeVideoId(request.YoutubeVideoId);

            var courseIds = ParseCursoIds(request.CursoIds, request.CursoIdsRaw);
            _context.RecursosCursos.RemoveRange(entity.RecursosCursos);

            if (courseIds.Count > 0)
            {
                var validCourseIds = await _context.CursosMedicina
                    .Where(c => courseIds.Contains(c.IdCurso))
                    .Select(c => c.IdCurso)
                    .ToListAsync();

                foreach (var courseId in validCourseIds)
                {
                    _context.RecursosCursos.Add(new RecursoCurso
                    {
                        IdBaseDatos = entity.IdBaseDatos,
                        IdCurso = courseId
                    });
                }
            }

            await _context.SaveChangesAsync();

            var updated = await _context.BasesDatosMedicas
                .Include(r => r.RecursosCursos)
                .ThenInclude(rc => rc.Curso)
                .AsNoTracking()
                .FirstAsync(r => r.IdBaseDatos == entity.IdBaseDatos);

            return Ok(MapToAdminDto(updated));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var resource = await _context.BasesDatosMedicas.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            var fileName = Path.GetFileName(resource.LogotipoUrl);
            if (!string.IsNullOrEmpty(fileName))
            {
                var rootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(rootPath, "recursos", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.BasesDatosMedicas.Remove(resource);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id:int}/toggle-activo")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var resource = await _context.BasesDatosMedicas.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            resource.EstadoActivo = !resource.EstadoActivo;
            if (!resource.EstadoActivo)
            {
                resource.MostrarEnHexagonos = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = resource.IdBaseDatos,
                estadoActivo = resource.EstadoActivo,
                mostrarEnHexagonos = resource.MostrarEnHexagonos
            });
        }

        [HttpPatch("{id:int}/toggle-destacado")]
        public async Task<IActionResult> ToggleDestacado(int id)
        {
            var resource = await _context.BasesDatosMedicas.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            if (!resource.MostrarEnHexagonos)
            {
                if (!resource.EstadoActivo)
                {
                    return BadRequest(new { message = "No se puede destacar un recurso inactivo. Actívelo primero." });
                }

                var count = await _context.BasesDatosMedicas.CountAsync(r => r.MostrarEnHexagonos && r.EstadoActivo && r.IdBaseDatos != id);
                if (count >= MaxDestacadosCount)
                {
                    return BadRequest(new { message = $"Límite alcanzado: Ya existen {MaxDestacadosCount} bases de datos destacadas en la cuadrícula de inicio. Desactive otra antes de destacar esta." });
                }
            }

            resource.MostrarEnHexagonos = !resource.MostrarEnHexagonos;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = resource.IdBaseDatos,
                mostrarEnHexagonos = resource.MostrarEnHexagonos
            });
        }

        [HttpPatch("{id:int}/toggle-mobile")]
        public async Task<IActionResult> ToggleMobile(int id)
        {
            var resource = await _context.BasesDatosMedicas.FindAsync(id);
            if (resource == null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            resource.TieneAppMovil = !resource.TieneAppMovil;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = resource.IdBaseDatos,
                tieneAppMovil = resource.TieneAppMovil
            });
        }

        [HttpGet("cursos")]
        public async Task<IActionResult> GetCursos()
        {
            var cursos = await _context.CursosMedicina
                .OrderBy(c => c.Ciclo)
                .ThenBy(c => c.NombreCurso)
                .Select(c => new CursoDto
                {
                    IdCurso = c.IdCurso,
                    NombreCurso = c.NombreCurso,
                    Ciclo = c.Ciclo,
                    CodigoCurso = c.CodigoCurso
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(cursos);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalRecursos = await _context.BasesDatosMedicas.CountAsync();
            var totalSuscripcion = await _context.BasesDatosMedicas.CountAsync(r => r.EsSuscripcion);
            var totalDestacados = await _context.BasesDatosMedicas.CountAsync(r => r.MostrarEnHexagonos && r.EstadoActivo);
            var totalCursosVinculados = await _context.RecursosCursos
                .Select(rc => rc.IdCurso)
                .Distinct()
                .CountAsync();

            var stats = new ResourceStatsDto
            {
                TotalRecursos = totalRecursos,
                TotalSuscripcion = totalSuscripcion,
                TotalDestacados = totalDestacados,
                TotalCursosVinculados = totalCursosVinculados
            };

            return Ok(stats);
        }

        private static ResourceAdminDto MapToAdminDto(BaseDatosMedica r)
        {
            return new ResourceAdminDto
            {
                IdBaseDatos = r.IdBaseDatos,
                NombreRecurso = r.NombreRecurso,
                LogotipoUrl = r.LogotipoUrl,
                DescripcionClinica = r.DescripcionClinica,
                EsSuscripcion = r.EsSuscripcion,
                TieneAppMovil = r.TieneAppMovil,
                UrlExterno = r.UrlExterno,
                EstadoActivo = r.EstadoActivo,
                MostrarEnHexagonos = r.MostrarEnHexagonos,
                YoutubeVideoId = r.YoutubeVideoId,
                Cursos = r.RecursosCursos
                    .OrderBy(rc => rc.Curso.Ciclo)
                    .ThenBy(rc => rc.Curso.NombreCurso)
                    .Select(rc => new ResourceCourseItemDto
                    {
                        IdCurso = rc.Curso.IdCurso,
                        NombreCurso = rc.Curso.NombreCurso,
                        Ciclo = rc.Curso.Ciclo,
                        CodigoCurso = rc.Curso.CodigoCurso
                    })
                    .ToList()
            };
        }

        private static List<int> ParseCursoIds(List<int>? cursoIds, string? cursoIdsRaw)
        {
            var ids = new HashSet<int>();
            if (cursoIds != null)
            {
                foreach (var id in cursoIds)
                {
                    if (id > 0) ids.Add(id);
                }
            }

            if (!string.IsNullOrWhiteSpace(cursoIdsRaw))
            {
                var parts = cursoIdsRaw.Split(new[] { ',', ';', '[', ']', ' ', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (int.TryParse(part.Trim(), out var parsed) && parsed > 0)
                    {
                        ids.Add(parsed);
                    }
                }
            }

            return ids.ToList();
        }

        private static string? CleanYoutubeVideoId(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var trimmed = input.Trim();
            var match = Regex.Match(
                trimmed,
                @"(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=|shorts\/))([\w-]{11})",
                RegexOptions.IgnoreCase);

            return match.Success ? match.Groups[1].Value : trimmed;
        }
    }
}
