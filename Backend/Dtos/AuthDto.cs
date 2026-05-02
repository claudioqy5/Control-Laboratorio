namespace ControlLaboratorio.API.Dtos
{
    public class LoginRequest
    {
        public string CodigoUniversitario { get; set; } = string.Empty;
        public string DNI { get; set; } = string.Empty;
        public string NombreRed { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        public int SesionId { get; set; }
    }

    public class AdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SetSessionLimitRequest
    {
        public int SesionId { get; set; }
        public DateTime NuevaHoraLimite { get; set; }
    }
}
