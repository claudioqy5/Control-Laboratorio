using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace ControlLaboratorio.API.DTOs
{
    public class ResourceCourseItemDto
    {
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int Ciclo { get; set; }
        public string? CodigoCurso { get; set; }
    }

    public class ResourceAdminDto
    {
        public int IdBaseDatos { get; set; }
        public string NombreRecurso { get; set; } = string.Empty;
        public string LogotipoUrl { get; set; } = string.Empty;
        public string DescripcionClinica { get; set; } = string.Empty;
        public bool EsSuscripcion { get; set; }
        public bool TieneAppMovil { get; set; }
        public string? UrlExterno { get; set; }
        public bool EstadoActivo { get; set; }
        public bool MostrarEnHexagonos { get; set; }
        public string? YoutubeVideoId { get; set; }
        public List<ResourceCourseItemDto> Cursos { get; set; } = new();
    }

    public class ResourcePublicDto
    {
        public int IdBaseDatos { get; set; }
        public string NombreRecurso { get; set; } = string.Empty;
        public string LogotipoUrl { get; set; } = string.Empty;
        public string DescripcionClinica { get; set; } = string.Empty;
        public bool EsSuscripcion { get; set; }
        public bool TieneAppMovil { get; set; }
        public string? UrlExterno { get; set; }
        public bool EstadoActivo { get; set; }
        public bool MostrarEnHexagonos { get; set; }
        public string? YoutubeVideoId { get; set; }
        public List<ResourceCourseItemDto> Cursos { get; set; } = new();
    }

    public class ResourceStatsDto
    {
        public int TotalRecursos { get; set; }
        public int TotalSuscripcion { get; set; }
        public int TotalDestacados { get; set; }
        public int TotalCursosVinculados { get; set; }
    }

    public class CursoDto
    {
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; } = string.Empty;
        public int Ciclo { get; set; }
        public string? CodigoCurso { get; set; }
    }

    public class CreateResourceFormDto
    {
        public string NombreRecurso { get; set; } = string.Empty;
        public string DescripcionClinica { get; set; } = string.Empty;
        public bool EsSuscripcion { get; set; } = true;
        public bool TieneAppMovil { get; set; } = false;
        public string? UrlExterno { get; set; }
        public bool MostrarEnHexagonos { get; set; } = false;
        public string? YoutubeVideoId { get; set; }
        public List<int>? CursoIds { get; set; }
        public string? CursoIdsRaw { get; set; }
        public IFormFile? Image { get; set; }
    }

    public class UpdateResourceFormDto
    {
        public string NombreRecurso { get; set; } = string.Empty;
        public string DescripcionClinica { get; set; } = string.Empty;
        public bool EsSuscripcion { get; set; } = true;
        public bool TieneAppMovil { get; set; } = false;
        public string? UrlExterno { get; set; }
        public bool EstadoActivo { get; set; } = true;
        public bool MostrarEnHexagonos { get; set; } = false;
        public string? YoutubeVideoId { get; set; }
        public List<int>? CursoIds { get; set; }
        public string? CursoIdsRaw { get; set; }
        public IFormFile? Image { get; set; }
    }

    public class SwapDestacadoDto
    {
        public int IdReemplazo { get; set; }
    }
}
