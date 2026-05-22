using System;

namespace ControlLaboratorio.API.Helpers
{
    public static class TimeHelper
    {
        public static DateTime GetPeruTime()
        {
            try
            {
                // En Windows el ID suele ser "SA Pacific Standard Time"
                // En Linux (AlmaLinux) el ID es "America/Lima"
                string timeZoneId = OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Lima";
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                // Fallback de seguridad: Forzar UTC-5 (Hora de Perú) si falla la zona horaria del sistema
                return DateTime.UtcNow.AddHours(-5);
            }
        }
    }
}
