using AgendaSaaS.Repositories;

namespace AgendaSaaS.Services;

public class TurnoExpirationService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MaxEdadPendiente = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TurnoExpirationService> _logger;

    public TurnoExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<TurnoExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var repo =
                    scope.ServiceProvider
                        .GetRequiredService<ITurnoRepository>();

                var expirados =
                    await repo.ExpirarPendientesAsync(
                        MaxEdadPendiente);

                if (expirados > 0)
                    _logger.LogInformation(
                        "{Count} turno(s) pendiente(s) expirados.",
                        expirados);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al expirar turnos pendientes.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }
}
