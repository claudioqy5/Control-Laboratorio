using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlLaboratorio.API.DTOs
{
    public class CursoAdminDto
    {
        public int IdCurso { get; set; }
        public string? CodigoCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int Ciclo { get; set; }
        public bool EstadoActivo { get; set; }
        public int TotalBasesAsociadas { get; set; }
        public List<string> BasesAsociadas { get; set; } = new();
    }

    public class CreateUpdateCursoDto
    {
        [StringLength(20)]
        public string? CodigoCurso { get; set; }

        [Required]
        [StringLength(150)]
        public string NombreCurso { get; set; } = string.Empty;

        [Range(1, 14)]
        public int Ciclo { get; set; }

        public bool EstadoActivo { get; set; } = true;
    }
}
