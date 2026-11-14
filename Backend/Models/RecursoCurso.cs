using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    [Table("t_recurso_curso")]
    public class RecursoCurso
    {
        [Column("id_base_datos")]
        public int IdBaseDatos { get; set; }

        [ForeignKey(nameof(IdBaseDatos))]
        public BaseDatosMedica BaseDatos { get; set; } = null!;

        [Column("id_curso")]
        public int IdCurso { get; set; }

        [ForeignKey(nameof(IdCurso))]
        public CursoMedicina Curso { get; set; } = null!;
    }
}
