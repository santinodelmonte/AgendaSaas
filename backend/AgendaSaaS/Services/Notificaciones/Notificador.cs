using System.Globalization;
using System.Threading.Channels;
using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;

namespace AgendaSaaS.Services.Notificaciones;

/// <summary>
/// Envía notificaciones push a todos los dispositivos de una manicurista.
/// Los avisos de reservas se encolan y los manda <see cref="NotificacionesWorker"/>
/// en segundo plano, para que la reserva de la clienta no espere ni falle por el push.
/// </summary>
public class Notificador
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-AR");

    private readonly Channel<(Guid TenantId, NotificacionPush Notificacion)> _cola =
        Channel.CreateBounded<(Guid, NotificacionPush)>(
            new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest });

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Notificador> _logger;

    public Notificador(
        IServiceScopeFactory scopeFactory,
        ILogger<Notificador> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ChannelReader<(Guid TenantId, NotificacionPush Notificacion)> Cola => _cola.Reader;

    public void AvisarNuevaReserva(Turno turno)
    {
        var fecha = turno.FechaHora.ToString("ddd d/M HH:mm", Es);

        var notificacion = new NotificacionPush(
            "Nueva reserva 💅",
            $"{turno.NombreCliente} · {fecha} · {turno.ServicioSolicitado}",
            "/admin/turnos-pendientes",
            $"turno-{turno.Id}");

        _cola.Writer.TryWrite((turno.TenantId, notificacion));
    }

    /// <summary>
    /// Envía a todos los dispositivos del tenant y borra las suscripciones vencidas.
    /// Devuelve a cuántos dispositivos llegó.
    /// </summary>
    public async Task<int> EnviarAsync(
        Guid tenantId,
        NotificacionPush notificacion,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();

        var repository =
            scope.ServiceProvider.GetRequiredService<IPushSuscripcionRepository>();

        var sender =
            scope.ServiceProvider.GetRequiredService<IPushSender>();

        var enviados = 0;

        foreach (var suscripcion in await repository.ObtenerPorTenantAsync(tenantId))
        {
            switch (await sender.EnviarAsync(suscripcion, notificacion, cancellationToken))
            {
                case ResultadoPush.Enviado:
                    enviados++;
                    break;

                case ResultadoPush.SuscripcionVencida:
                    _logger.LogInformation(
                        "Suscripción push {Id} vencida: se elimina.", suscripcion.Id);
                    await repository.EliminarPorIdAsync(suscripcion.Id);
                    break;
            }
        }

        return enviados;
    }
}

public class NotificacionesWorker : BackgroundService
{
    private readonly Notificador _notificador;
    private readonly ILogger<NotificacionesWorker> _logger;

    public NotificacionesWorker(
        Notificador notificador,
        ILogger<NotificacionesWorker> logger)
    {
        _notificador = notificador;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (tenantId, notificacion) in _notificador.Cola.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _notificador.EnviarAsync(tenantId, notificacion, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error al enviar notificaciones del tenant {TenantId}.", tenantId);
            }
        }
    }
}
