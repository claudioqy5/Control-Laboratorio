using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    public class Sesion
    {
        [Key]
        public int SesionID { get; set; }

        [Required]
        public int AlumnoID { get; set; }

        [Required]
        public int EquipoID { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public DateTime HoraInicio { get; set; } = DateTime.Now;

        public DateTime? HoraFin { get; set; }

        public DateTime? HoraLimite { get; set; }

        [ForeignKey("AlumnoID")]
        public Alumno? Alumno { get; set; }

        [ForeignKey("EquipoID")]
        public Equipo? Equipo { get; set; }
    }
}
