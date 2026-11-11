using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlLaboratorio.API.Models
{
    public enum EstadoObjeto : byte
    {
        EnCustodia = 1,
        Devuelto = 2,
        Dispuesto = 4
    }

    public enum CategoriaObjeto : byte
    {
        Electronica = 1,
        Documentos = 2,
        Estudio = 3,
        Prendas = 4,
        Otros = 5
    }

    [Table("OBJETOS_PERDIDOS")]
    public class ObjetoPerdido
    {
        [Key]
        public int IdObjeto { get; set; }

        [Required]
        [MaxLength(150)]
        public string Titulo { get; set; } = null!;

        [Required]
        public CategoriaObjeto Categoria { get; set; }

        [Required]
        public string Descripcion { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string LugarHallazgo { get; set; } = null!;

        [Required]
        public DateTime FechaHoraHallazgo { get; set; }

        [Required]
        public EstadoObjeto EstadoObjeto { get; set; } = EstadoObjeto.EnCustodia;

        [Required]
        public int IdUsuarioHallazgo { get; set; }

        [Required]
        [MaxLength(150)]
        public string NombrePersonalHallazgo { get; set; } = null!;

        public DateTime? FechaHoraDevolucion { get; set; }

        public int? AlumnoID { get; set; }

        [MaxLength(20)]
        public string? DocumentoReceptor { get; set; }

        [MaxLength(20)]
        public string? CodigoURPReceptor { get; set; }

        public int? IdUsuarioEntrega { get; set; }

        [MaxLength(150)]
        public string? NombrePersonalEntrega { get; set; }

        public string? ObservacionesEntrega { get; set; }

        public DateTime? FechaBaja { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [ForeignKey("AlumnoID")]
        public Alumno? Alumno { get; set; }

        [ForeignKey("IdUsuarioHallazgo")]
        public UsuarioAdmin? UsuarioHallazgo { get; set; }

        [ForeignKey("IdUsuarioEntrega")]
        public UsuarioAdmin? UsuarioEntrega { get; set; }
    }
}
