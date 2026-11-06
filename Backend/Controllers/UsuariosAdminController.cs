using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Dtos;
using ControlLaboratorio.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ControlLaboratorio.API.Controllers
{
    [Route("api/admin/usuarios")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class UsuariosAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsuariosAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioAdminDto>>> GetUsuarios()
        {
            var usuarios = await _context.UsuariosAdmin
                .OrderBy(u => u.IdUsuarioAdmin)
                .Select(u => new UsuarioAdminDto
                {
                    IdUsuarioAdmin = u.IdUsuarioAdmin,
                    Username = u.Username,
                    Nombres = u.Nombres,
                    Correo = u.Correo,
                    Rol = u.Rol,
                    RolNombre = u.Rol.ToString(),
                    EstadoActivo = u.EstadoActivo,
                    FechaCreacion = u.FechaCreacion,
                    UltimoAcceso = u.UltimoAcceso
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioAdminDto>> GetUsuarioById(int id)
        {
            var u = await _context.UsuariosAdmin.FindAsync(id);
            if (u == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            return Ok(new UsuarioAdminDto
            {
                IdUsuarioAdmin = u.IdUsuarioAdmin,
                Username = u.Username,
                Nombres = u.Nombres,
                Correo = u.Correo,
                Rol = u.Rol,
                RolNombre = u.Rol.ToString(),
                EstadoActivo = u.EstadoActivo,
                FechaCreacion = u.FechaCreacion,
                UltimoAcceso = u.UltimoAcceso
            });
        }

        [HttpPost]
        public async Task<ActionResult<UsuarioAdminDto>> CreateUsuario([FromBody] CreateUsuarioAdminRequest request)
        {

            var username = request.Username?.Trim() ?? string.Empty;
            var password = request.Password?.Trim() ?? string.Empty;
            var nombres = request.Nombres?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                return BadRequest(new { message = "El nombre de usuario debe tener al menos 3 caracteres." });
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });
            }

            if (string.IsNullOrWhiteSpace(nombres))
            {
                return BadRequest(new { message = "Los nombres son requeridos." });
            }

            bool exists = await _context.UsuariosAdmin.AnyAsync(u => u.Username.ToLower() == username.ToLower());
            if (exists)
            {
                return Conflict(new { message = "El nombre de usuario ya se encuentra registrado." });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, 11);

            var nuevoUsuario = new UsuarioAdmin
            {
                Username = username,
                PasswordHash = passwordHash,
                Nombres = nombres,
                Correo = request.Correo?.Trim(),
                Rol = request.Rol,
                EstadoActivo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.UsuariosAdmin.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            var dto = new UsuarioAdminDto
            {
                IdUsuarioAdmin = nuevoUsuario.IdUsuarioAdmin,
                Username = nuevoUsuario.Username,
                Nombres = nuevoUsuario.Nombres,
                Correo = nuevoUsuario.Correo,
                Rol = nuevoUsuario.Rol,
                RolNombre = nuevoUsuario.Rol.ToString(),
                EstadoActivo = nuevoUsuario.EstadoActivo,
                FechaCreacion = nuevoUsuario.FechaCreacion,
                UltimoAcceso = nuevoUsuario.UltimoAcceso
            };

            return CreatedAtAction(nameof(GetUsuarioById), new { id = dto.IdUsuarioAdmin }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, [FromBody] UpdateUsuarioAdminRequest request)
        {
            var usuario = await _context.UsuariosAdmin.FindAsync(id);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            if (string.IsNullOrWhiteSpace(request.Nombres))
            {
                return BadRequest(new { message = "Los nombres son requeridos." });
            }

            usuario.Nombres = request.Nombres.Trim();
            usuario.Correo = request.Correo?.Trim();
            usuario.Rol = request.Rol;

            await _context.SaveChangesAsync();

            return Ok(new UsuarioAdminDto
            {
                IdUsuarioAdmin = usuario.IdUsuarioAdmin,
                Username = usuario.Username,
                Nombres = usuario.Nombres,
                Correo = usuario.Correo,
                Rol = usuario.Rol,
                RolNombre = usuario.Rol.ToString(),
                EstadoActivo = usuario.EstadoActivo,
                FechaCreacion = usuario.FechaCreacion,
                UltimoAcceso = usuario.UltimoAcceso
            });
        }

        [HttpPatch("{id}/estado")]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var usuario = await _context.UsuariosAdmin.FindAsync(id);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (int.TryParse(currentUserIdClaim, out var currentId) && currentId == id)
            {
                return BadRequest(new { message = "No es posible desactivar su propia cuenta en sesión activa." });
            }

            usuario.EstadoActivo = !usuario.EstadoActivo;

            if (!usuario.EstadoActivo)
            {
                var activeTokens = await _context.RefreshTokens
                    .Where(t => t.IdUsuarioAdmin == id && t.RevokedAt == null)
                    .ToListAsync();

                foreach (var t in activeTokens)
                {
                    t.RevokedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Estado actualizado.", estadoActivo = usuario.EstadoActivo });
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NuevaPassword) || request.NuevaPassword.Length < 6)
            {
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });
            }

            var usuario = await _context.UsuariosAdmin.FindAsync(id);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword.Trim(), 11);

            var activeTokens = await _context.RefreshTokens
                .Where(t => t.IdUsuarioAdmin == id && t.RevokedAt == null)
                .ToListAsync();

            foreach (var t in activeTokens)
            {
                t.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada exitosamente." });
        }
    }
}
