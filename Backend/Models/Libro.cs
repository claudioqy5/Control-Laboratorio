using System.ComponentModel.DataAnnotations;

namespace ControlLaboratorio.API.Models
{
    public class Libro
    {
        [Key]
        public int LibroID { get; set; }

        [Required]
        [StringLength(50)]
        public string NroRegistro { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string CodigoBarras { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string NroClasificacion { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Autor { get; set; } = string.Empty;

        [StringLength(10)]
        public string Anio { get; set; } = string.Empty;

        [StringLength(150)]
        public string Editorial { get; set; } = string.Empty;

        [StringLength(50)]
        public string Edicion { get; set; } = string.Empty;

        // Will store base64 string or image URL
        public string? Portada { get; set; }

        [StringLength(100)]
        public string Categoria { get; set; } = string.Empty;

        [StringLength(50)]
        public string Idioma { get; set; } = "Español";

        // Ubicación Física en Biblioteca
        public int? Estante { get; set; } // 1 al 7
        [StringLength(1)]
        public string? Cara { get; set; } // 'A' o 'B'
        public int? Piso { get; set; } // 1 al 6

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Disponible"; // Disponible, Prestado, Reservado

        public string? Resumen { get; set; }

        public int Paginas { get; set; }

        // Navigation properties
        public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
        public ICollection<Favorito> Favoritos { get; set; } = new List<Favorito>();
    }
}
