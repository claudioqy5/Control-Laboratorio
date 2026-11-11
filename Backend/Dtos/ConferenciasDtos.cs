namespace ControlLaboratorio.API.DTOs
{
    public class PreInscripcionDto
    {
        public string TipoParticipante { get; set; } = "Estudiante";
        public string? TipoDocumento { get; set; } = "DNI";
        public string? NumeroDocumento { get; set; }
        public string? CodigoURP { get; set; }
        public string? Nombres { get; set; }
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }
        public string? Correo { get; set; }
    }

    public class MarkAttendanceDto
    {
        public string DocumentoOCodigo { get; set; } = string.Empty;
    }

    public class QuickEditParticipanteDto
    {
        public string? NumeroDocumento { get; set; }
        public string? TipoDocumento { get; set; }
        public string? CodigoURP { get; set; }
        public string? TipoParticipante { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string? ApellidoMaterno { get; set; }
        public string? Correo { get; set; }
    }
}
