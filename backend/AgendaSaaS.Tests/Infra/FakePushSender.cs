using System.Collections.Concurrent;
using AgendaSaaS.Entities;
using AgendaSaaS.Services.Notificaciones;

namespace AgendaSaaS.Tests.Infra;

public sealed class FakePushSender : IPushSender
{
    public ConcurrentQueue<(string Endpoint, NotificacionPush Notificacion)> Enviados { get; } = new();

    // Endpoints para los que el "servicio de push" responde 410 Gone.
    public ConcurrentDictionary<string, bool> Vencidos { get; } = new();

    public Task<ResultadoPush> EnviarAsync(
        PushSuscripcion suscripcion,
        NotificacionPush notificacion,
        CancellationToken cancellationToken = default)
    {
        Enviados.Enqueue((suscripcion.Endpoint, notificacion));

        return Task.FromResult(
            Vencidos.ContainsKey(suscripcion.Endpoint)
                ? ResultadoPush.SuscripcionVencida
                : ResultadoPush.Enviado);
    }

    public List<NotificacionPush> Para(string endpoint) =>
        Enviados.Where(e => e.Endpoint == endpoint).Select(e => e.Notificacion).ToList();
}
