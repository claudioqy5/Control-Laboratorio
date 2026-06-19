using System.ComponentModel.DataAnnotations;

namespace ControlLaboratorio.API.Models
{
    public class ScanLog
    {
        [Key]
        public int ScanLogID { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? RealizadoPor { get; set; }

        [Required]
        public bool IsExitoso { get; set; } = true;

        [StringLength(255)]
        public string? Mensaje { get; set; }

        [StringLength(50)]
        public string? AlumnoCodigo { get; set; }

        [StringLength(150)]
        public string? AlumnoNombre { get; set; }
    }
}
