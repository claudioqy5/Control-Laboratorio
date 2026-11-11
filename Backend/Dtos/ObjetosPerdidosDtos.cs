using System;
using System.ComponentModel.DataAnnotations;
using ControlLaboratorio.API.Models;

namespace ControlLaboratorio.API.DTOs
{
    public class RegistrarObjetoDto
    {
        [Required]
        [MaxLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public CategoriaObjeto Categoria { get; set; }

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string LugarHallazgo { get; set; } = string.Empty;

        [Required]
        public DateTime FechaHoraHallazgo { get; set; }

        [Required]
        public int IdUsuarioHallazgo { get; set; }
    }

    public class DevolverObjetoDto
    {
        [Required]
        [MaxLength(20)]
        public string CodigoUniversitario { get; set; } = string.Empty;

        [Required]
        public int IdUsuarioEntrega { get; set; }

        public string? Observaciones { get; set; }
    }

    public class ObjetoPerdidoDto
    {
        public int IdObjeto { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public CategoriaObjeto Categoria { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string LugarHallazgo { get; set; } = string.Empty;
        public DateTime FechaHoraHallazgo { get; set; }
        public EstadoObjeto EstadoObjeto { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public int IdUsuarioHallazgo { get; set; }
        public string NombrePersonalHallazgo { get; set; } = string.Empty;
        public DateTime? FechaHoraDevolucion { get; set; }
        public int? AlumnoID { get; set; }
        public string? DocumentoReceptor { get; set; }
        public string? CodigoURPReceptor { get; set; }
        public int? IdUsuarioEntrega { get; set; }
        public string? NombrePersonalEntrega { get; set; }
        public string? ObservacionesEntrega { get; set; }
        public DateTime? FechaBaja { get; set; }
        public int DiasRestantes { get; set; }
        public bool EsVencido { get; set; }
    }

    public class ObjetosPerdidosKpisDto
    {
        public int TotalCustodia { get; set; }
        public int TotalDevueltos { get; set; }
        public int TotalVencidos { get; set; }
        public int TotalBajas { get; set; }
        public int TotalRegistrados { get; set; }
    }
}
