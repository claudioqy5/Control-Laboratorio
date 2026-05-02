using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    public class Alumno
    {
        [Key]
        public int AlumnoID { get; set; }

        [Required]
        [StringLength(20)]
        public string CodigoUniversitario { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string DNI { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ApellidoMaterno { get; set; } = string.Empty;

        [StringLength(100)]
        public string Carrera { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? CorreoInstitucional { get; set; }

        [StringLength(100)]
        public string? CorreoPersonal { get; set; }

        public bool Estado { get; set; } = true;

        public ICollection<Sesion> Sesiones { get; set; } = new List<Sesion>();
    }
}
