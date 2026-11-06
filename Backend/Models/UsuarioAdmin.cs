using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    [Table("UsuariosAdmin")]
    public class UsuarioAdmin
    {
        [Key]
        public int IdUsuarioAdmin { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Nombres { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Correo { get; set; }

        public RolAdmin Rol { get; set; } = RolAdmin.SuperAdmin;

        public bool EstadoActivo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? UltimoAcceso { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
