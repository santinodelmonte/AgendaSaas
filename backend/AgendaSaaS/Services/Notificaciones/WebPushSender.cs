using System.Net;
using System.Text.Json;
using AgendaSaaS.Entities;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;

namespace AgendaSaaS.Services.Notificaciones;

/// <summary>
/// Envía con el estándar Web Push (RFC 8030/8291/8292): payload cifrado con
/// aes128gcm, que es el único que aceptan los iPhone, y firma VAPID.
/// </summary>
public class WebPushSender : IPushSender
{
    // Una reserva que no llegó en un día ya no sirve como aviso.
    private const int TtlSegundos = 24 * 60 * 60;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly VapidKeys _vapidKeys;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(
        VapidKeys vapidKeys,
        IHttpClientFactory httpClientFactory,
        ILogger<WebPushSender> logger)
    {
        _vapidKeys = vapidKeys;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ResultadoPush> EnviarAsync(
        PushSuscripcion suscripcion,
        NotificacionPush notificacion,
        CancellationToken cancellationToken = default)
    {
        var vapid = await _vapidKeys.ObtenerAsync();

        using var autenticacion = new VapidAuthentication(vapid.PublicKey, vapid.PrivateKey)
        {
            Subject = vapid.Subject
        };

        var client = new PushServiceClient(_httpClientFactory.CreateClient(nameof(WebPushSender)))
        {
            DefaultAuthentication = autenticacion,
            DefaultAuthenticationScheme = VapidAuthenticationScheme.Vapid
        };

        var subscription = new PushSubscription { Endpoint = suscripcion.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, suscripcion.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, suscripcion.Auth);

        var mensaje = new PushMessage(JsonSerializer.Serialize(notificacion, Json))
        {
            TimeToLive = TtlSegundos,
            Urgency = PushMessageUrgency.High
        };

        try
        {
            await client.RequestPushMessageDeliveryAsync(subscription, mensaje, cancellationToken);
            return ResultadoPush.Enviado;
        }
        catch (PushServiceClientException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return ResultadoPush.SuscripcionVencida;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(
                ex,
                "No se pudo enviar el push a la suscripción {Id}.",
                suscripcion.Id);

            return ResultadoPush.Error;
        }
    }
}
