using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    public class Prestamo
    {
        [Key]
        public int PrestamoID { get; set; }

        [Required]
        public int AlumnoID { get; set; }

        [Required]
        public int LibroID { get; set; }

        [Required]
        public DateTime FechaPrestamo { get; set; } = DateTime.Now;

        [Required]
        public DateTime FechaDevolucion { get; set; }

        public DateTime? FechaEntregado { get; set; }

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Activo"; // Activo, Devuelto, Vencido, Renovado

        // Navigation properties
        [ForeignKey("AlumnoID")]
        public Alumno? Alumno { get; set; }

        [ForeignKey("LibroID")]
        public Libro? Libro { get; set; }

        public ICollection<Multa> Multas { get; set; } = new List<Multa>();
    }
}
