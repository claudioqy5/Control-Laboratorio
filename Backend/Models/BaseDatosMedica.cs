using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    [Table("t_base_datos_medica")]
    public class BaseDatosMedica
    {
        [Key]
        [Column("id_base_datos")]
        public int IdBaseDatos { get; set; }

        [Required]
        [Column("nombre_recurso")]
        [StringLength(150)]
        public string NombreRecurso { get; set; } = null!;

        [Required]
        [Column("logotipo_url")]
        [StringLength(500)]
        public string LogotipoUrl { get; set; } = null!;

        [Required]
        [Column("descripcion_clinica")]
        [StringLength(1000)]
        public string DescripcionClinica { get; set; } = null!;

        [Column("es_suscripcion")]
        public bool EsSuscripcion { get; set; } = true;

        [Column("tiene_app_movil")]
        public bool TieneAppMovil { get; set; } = false;

        [Column("url_externo")]
        [StringLength(500)]
        public string? UrlExterno { get; set; }

        [Column("estado_activo")]
        public bool EstadoActivo { get; set; } = true;

        [Column("mostrar_en_hexagonos")]
        public bool MostrarEnHexagonos { get; set; } = false;

        [Column("youtube_video_id")]
        [StringLength(50)]
        public string? YoutubeVideoId { get; set; }

        public ICollection<RecursoCurso> RecursosCursos { get; set; } = new List<RecursoCurso>();
    }
}
