using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Token { get; set; } = string.Empty;

        public int IdUsuarioAdmin { get; set; }

        [ForeignKey(nameof(IdUsuarioAdmin))]
        public UsuarioAdmin? UsuarioAdmin { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAt { get; set; }

        [MaxLength(150)]
        public string? ReplacedByToken { get; set; }
    }
}
