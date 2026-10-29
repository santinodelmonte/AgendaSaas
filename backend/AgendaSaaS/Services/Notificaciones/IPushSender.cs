using AgendaSaaS.Entities;

namespace AgendaSaaS.Services.Notificaciones;

public enum ResultadoPush
{
    Enviado,

    // El servicio de push respondió que la suscripción ya no existe
    // (la usuaria desinstaló la app o revocó el permiso): hay que borrarla.
    SuscripcionVencida,

    Error
}

public interface IPushSender
{
    Task<ResultadoPush> EnviarAsync(
        PushSuscripcion suscripcion,
        NotificacionPush notificacion,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contenido que recibe el service worker del frontend (frontend/public/sw.js).
/// </summary>
public record NotificacionPush(
    string Titulo,
    string Cuerpo,
    string Url,
    string Tag);
