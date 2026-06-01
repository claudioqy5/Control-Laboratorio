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

        // Tolerancia de desconexión (Agent pings every 5 seconds)
        // Usamos 25 segundos para evitar falsos positivos por lag de red
        private readonly TimeSpan _timeoutThreshold = TimeSpan.FromSeconds(25);

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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el servicio de monitoreo de sesiones.");
                }

                // Ejecutar cada 10 segundos para detectar rápidamente caídas
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task VerificarConexionesActivasAsync(CancellationToken stoppingToken)
        {
            var now = TimeHelper.GetPeruTime();
            var sesionesDesconectadas = new List<int>();

            // Identificar sesiones que no han reportado heartbeat recientemente
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

                    // La hora final será la última vez que tuvimos señales de vida (o ahora mismo)
                    // Usaremos la última hora de ping si es posible, pero fallamos seguro a "now"
                    if (AuthController.ActiveSessionPings.TryGetValue(sesionId, out var lastPing))
                    {
                        sesion.HoraFin = lastPing;
                    }
                    else
                    {
                        sesion.HoraFin = now;
                    }
                    
                    // Actualizar el total consumido por el alumno y bloquear si alcanzó su límite
                    if (sesion.Alumno != null)
                    {
                        // Guardar los cambios de la sesión para que se incluya en la suma (al estar en la misma transacción EF trackea)
                        var sesionesHoy = await dbContext.Sesiones
                            .Where(s => s.AlumnoID == sesion.AlumnoID && s.Fecha.Date == now.Date && s.HoraFin != null)
                            .ToListAsync(stoppingToken);

                        // Como EF trackea la entidad actual, agregamos la sesión actual a la suma si no viene de BD
                        double segundosConsumidosHoy = sesionesHoy.Where(s => s.SesionID != sesionId).Sum(s => (s.HoraFin!.Value - s.HoraInicio).TotalSeconds);
                        segundosConsumidosHoy += (sesion.HoraFin.Value - sesion.HoraInicio).TotalSeconds;

                        if (segundosConsumidosHoy >= (3 * 3600 - 60))
                        {
                            sesion.Alumno.Estado = false;
                        }
                        else
                        {
                            sesion.Alumno.Estado = true;
                        }
                    }
                }

                // Removemos del diccionario independientemente de si existía en BD o ya estaba cerrada
                AuthController.ActiveSessionPings.TryRemove(sesionId, out _);
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
