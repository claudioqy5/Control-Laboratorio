using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    [Table("PARTICIPACIONES_CONFERENCIA")]
    public class ParticipacionConferencia
    {
        [Key]
        public int IdParticipacion { get; set; }

        [Required]
        public int IdConferencia { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoParticipante { get; set; } = "Estudiante";

        [Required]
        [StringLength(20)]
        public string TipoDocumento { get; set; } = "DNI";

        [Required]
        [StringLength(20)]
        public string NumeroDocumento { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CodigoURP { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ApellidoPaterno { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ApellidoMaterno { get; set; }

        [Required]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;

        public DateTime FechaHoraInscripcion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaHoraAsistencia { get; set; }

        public byte EstadoParticipacion { get; set; } = 1;

        public int? AlumnoID { get; set; }

        [ForeignKey("IdConferencia")]
        public ConferenciaMedica? Conferencia { get; set; }

        [ForeignKey("AlumnoID")]
        public Alumno? Alumno { get; set; }
    }
}
