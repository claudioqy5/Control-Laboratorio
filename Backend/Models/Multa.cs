using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    public class Multa
    {
        [Key]
        public int MultaID { get; set; }

        [Required]
        public int AlumnoID { get; set; }

        [Required]
        public int PrestamoID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Pagado

        [Required]
        public DateTime FechaEmision { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("AlumnoID")]
        public Alumno? Alumno { get; set; }

        [ForeignKey("PrestamoID")]
        public Prestamo? Prestamo { get; set; }
    }
}
