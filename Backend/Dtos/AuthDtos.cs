using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.Dtos
{
    public class AdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AdminLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UsuarioAdminDto Usuario { get; set; } = null!;
        public bool EsLegacy { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class CreateUsuarioAdminRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public RolAdmin Rol { get; set; }
    }

    public class UpdateUsuarioAdminRequest
    {
        public string Nombres { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public RolAdmin Rol { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string NuevaPassword { get; set; } = string.Empty;
    }

    public class UsuarioAdminDto
    {
        public int IdUsuarioAdmin { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public RolAdmin Rol { get; set; }
        public string RolNombre { get; set; } = string.Empty;
        public bool EstadoActivo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }
}
