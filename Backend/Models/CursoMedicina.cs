using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    [Table("t_curso_medicina")]
    public class CursoMedicina
    {
        [Key]
        [Column("id_curso")]
        public int IdCurso { get; set; }

        [Required]
        [Column("nombre_curso")]
        [StringLength(150)]
        public string NombreCurso { get; set; } = null!;

        [Column("ciclo")]
        public int Ciclo { get; set; }

        [Column("codigo_curso")]
        [StringLength(20)]
        public string? CodigoCurso { get; set; }

        [Column("estado_activo")]
        public bool EstadoActivo { get; set; } = true;

        public ICollection<RecursoCurso> RecursosCursos { get; set; } = new List<RecursoCurso>();
    }
}
