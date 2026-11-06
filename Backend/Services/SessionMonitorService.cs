using ControlLaboratorio.API.Controllers;
using ControlLaboratorio.API.Data;
using ControlLaboratorio.API.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ControlLaboratorio.API.Services
{
    public class SessionMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionMonitorService> _logger;
        private readonly TimeSpan _timeoutThreshold = TimeSpan.FromSeconds(90);
        private DateTime _lastTokenCleanup = DateTime.MinValue;

        public SessionMonitorService(IServiceProvider serviceProvider, ILogger<SessionMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Monitoreo de Sesiones (Heartbeat) iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarConexionesActivasAsync(stoppingToken);

                    if ((DateTime.UtcNow - _lastTokenCleanup) > TimeSpan.FromHours(1))
                    {
                        _lastTokenCleanup = DateTime.UtcNow;
                        await PurgarTokensExpiradosAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el servicio de monitoreo de sesiones.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task VerificarConexionesActivasAsync(CancellationToken stoppingToken)
        {
            var now = TimeHelper.GetPeruTime();
            var sesionesDesconectadas = new List<int>();

            foreach (var kvp in AuthController.ActiveSessionPings)
            {
                if ((now - kvp.Value) > _timeoutThreshold)
                {
                    sesionesDesconectadas.Add(kvp.Key);
                }
            }

            if (!sesionesDesconectadas.Any())
                return;

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var sesionId in sesionesDesconectadas)
            {
                var sesion = await dbContext.Sesiones
                    .Include(s => s.Alumno)
                    .Include(s => s.Equipo)
                    .FirstOrDefaultAsync(s => s.SesionID == sesionId, stoppingToken);

                if (sesion != null && sesion.HoraFin == null)
                {
                    _logger.LogWarning("Equipo {Equipo} (Sesion {SesionId}) se desconectó inesperadamente. Cerrando sesión.", 
                        sesion.Equipo?.NombreRed ?? "Desconocido", sesionId);

                    if (AuthController.ActiveSessionPings.TryGetValue(sesionId, out var lastPing))
                    {
                        sesion.HoraFin = lastPing;
                    }
                    else
                    {
                        sesion.HoraFin = now;
                    }
                    
                    if (sesion.Alumno != null)
                    {
                        var sesionesHoy = await dbContext.Sesiones
                            .Where(s => s.AlumnoID == sesion.AlumnoID && s.Fecha.Date == now.Date && s.HoraFin != null)
                            .ToListAsync(stoppingToken);

                        double segundosConsumidosHoy = sesionesHoy.Where(s => s.SesionID != sesionId).Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);
                        segundosConsumidosHoy += (sesion.HoraFin.Value - sesion.HoraInicio).TotalSeconds;

                        if (segundosConsumidosHoy >= (sesion.Alumno.LimiteDiarioSegundos - 60))
                        {
                            sesion.Alumno.Estado = false;
                        }
                        else
                        {
                            sesion.Alumno.Estado = true;
                        }
                    }
                }

                AuthController.ActiveSessionPings.TryRemove(sesionId, out _);
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }

        private async Task PurgarTokensExpiradosAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cutoff = DateTime.UtcNow.AddHours(-48);

                var tokensToDelete = await dbContext.RefreshTokens
                    .Where(t => (t.RevokedAt != null || t.ExpiresAt < DateTime.UtcNow) && t.ExpiresAt < cutoff)
                    .ToListAsync(stoppingToken);

                if (tokensToDelete.Any())
                {
                    dbContext.RefreshTokens.RemoveRange(tokensToDelete);
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Se purgaron {Count} refresh tokens expirados o revocados.", tokensToDelete.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al purgar refresh tokens antiguos.");
            }
        }
    }
}
