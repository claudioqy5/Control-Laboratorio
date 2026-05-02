using System.ComponentModel.DataAnnotations;

namespace ControlLaboratorio.API.Models
{
    public class Equipo
    {
        [Key]
        public int EquipoID { get; set; }

        [Required]
        [StringLength(50)]
        public string NombreRed { get; set; } = string.Empty;

        [StringLength(100)]
        public string Ubicacion { get; set; } = string.Empty;

        public bool Estado { get; set; } = true;

        public int? PosicionMapa { get; set; }

        public ICollection<Sesion> Sesiones { get; set; } = new List<Sesion>();
    }
}
