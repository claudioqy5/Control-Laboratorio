using ControlLaboratorio.API.Data;
using Microsoft.EntityFrameworkCore;
using ControlLaboratorio.API.Helpers;

namespace ControlLaboratorio.API.Services
{
    public class ReactivacionAlumnosService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReactivacionAlumnosService> _logger;

        public ReactivacionAlumnosService(IServiceProvider serviceProvider, ILogger<ReactivacionAlumnosService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Reactivación de Alumnos iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcesarReactivacionAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ocurrido en el servicio de reactivación de alumnos.");
                }

                // Ejecutar cada 5 minutos
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcesarReactivacionAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Obtener alumnos inactivos
            var alumnosInactivos = await dbContext.Alumnos
                .Include(a => a.Sesiones)
                .Where(a => a.Estado == false)
                .ToListAsync(stoppingToken);

            int reactivadosCount = 0;

            foreach (var alumno in alumnosInactivos)
            {
                // Buscar la última sesión terminada del alumno
                var ultimaSesion = alumno.Sesiones
                    .Where(s => s.HoraFin.HasValue)
                    .OrderByDescending(s => s.HoraFin)
                    .FirstOrDefault();

                if (ultimaSesion != null && ultimaSesion.HoraFin.HasValue)
                {
                    // Si la última sesión fue ayer o antes, lo reactivamos para darle sus nuevas 3 horas del día
                    if (ultimaSesion.HoraFin.Value.Date < TimeHelper.GetPeruTime().Date)
                    {
                        alumno.Estado = true;
                        reactivadosCount++;
                        _logger.LogInformation("Alumno {Codigo} reactivado automáticamente. Su última sesión fue el {HoraFin}.", 
                            alumno.CodigoUniversitario, ultimaSesion.HoraFin.Value);
                    }
                }
            }

            if (reactivadosCount > 0)
            {
                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Se reactivaron {Count} alumnos en este ciclo.", reactivadosCount);
            }
        }
    }
}
