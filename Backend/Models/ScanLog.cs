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
    }
}
