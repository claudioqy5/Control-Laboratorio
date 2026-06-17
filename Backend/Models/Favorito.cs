using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    public class Favorito
    {
        [Key]
        public int FavoritoID { get; set; }

        [Required]
        public int AlumnoID { get; set; }

        [Required]
        public int LibroID { get; set; }

        // Navigation properties
        [ForeignKey("AlumnoID")]
        public Alumno? Alumno { get; set; }

        [ForeignKey("LibroID")]
        public Libro? Libro { get; set; }
    }
}
