using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    [Table("t_conferencia_medica")]
    public class ConferenciaMedica
    {
        [Key]
        [Column("id_conferencia")]
        public int IdConferencia { get; set; }

        [Required]
        [Column("titulo_evento")]
        [StringLength(250)]
        public string TituloEvento { get; set; } = string.Empty;

        [Required]
        [Column("expositor_ponente")]
        [StringLength(200)]
        public string ExpositorPonente { get; set; } = string.Empty;

        [Column("entidad_editorial")]
        [StringLength(150)]
        public string? EntidadEditorial { get; set; }

        [Required]
        [Column("fecha_hora_inicio")]
        public DateTime FechaHoraInicio { get; set; }

        [Required]
        [Column("fecha_hora_fin")]
        public DateTime FechaHoraFin { get; set; }

        [Required]
        [Column("modalidad")]
        [StringLength(20)]
        public string Modalidad { get; set; } = "Presencial";

        [Required]
        [Column("ubicacion_o_enlace")]
        [StringLength(500)]
        public string Ubicacion_o_Enlace { get; set; } = string.Empty;

        [Column("asistencia_abierta")]
        public bool AsistenciaAbierta { get; set; } = false;

        [Required]
        [Column("estado_evento")]
        [StringLength(20)]
        public string EstadoEvento { get; set; } = "Programado";

        [Column("auto_purgar_30_dias")]
        public bool AutoPurgar30Dias { get; set; } = true;

        [Column("fecha_caducidad_purge")]
        public DateTime? FechaCaducidadPurge { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public ICollection<ParticipacionConferencia> Participaciones { get; set; } = new List<ParticipacionConferencia>();
    }
}
