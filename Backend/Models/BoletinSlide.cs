using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    public enum FormatoSlide : byte
    {
        Vertical = 1,
        Bipartito = 2
    }

    [Table("BOLETIN_SLIDES")]
    public class BoletinSlide
    {
        [Key]
        public int IdSlide { get; set; }

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = null!;

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        [Required]
        [StringLength(500)]
        public string ImagenUrl { get; set; } = null!;

        public FormatoSlide Formato { get; set; } = FormatoSlide.Vertical;

        [StringLength(500)]
        public string? EnlaceUrl { get; set; }
    }
}
