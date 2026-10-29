using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using AgendaSaaS.Services.Notificaciones;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/notificaciones")]
public class AdminNotificacionesController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IPushSuscripcionRepository _repository;
    private readonly VapidKeys _vapidKeys;
    private readonly Notificador _notificador;

    public AdminNotificacionesController(
        ITenantProvider tenantProvider,
        IPushSuscripcionRepository repository,
        VapidKeys vapidKeys,
        Notificador notificador)
    {
        _tenantProvider = tenantProvider;
        _repository = repository;
        _vapidKeys = vapidKeys;
        _notificador = notificador;
    }

    // El navegador la necesita para crear la suscripción (applicationServerKey).
    [HttpGet("clave-publica")]
    public async Task<IActionResult> ClavePublica()
    {
        var vapid = await _vapidKeys.ObtenerAsync();
        return Ok(new { publicKey = vapid.PublicKey });
    }

    [HttpPost("suscripciones")]
    public async Task<IActionResult> Suscribir(SuscripcionPushRequest request)
    {
        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            request.Endpoint.Length > 2000)
            return BadRequest("Endpoint de suscripción inválido.");

        if (string.IsNullOrWhiteSpace(request.Keys?.P256dh) || request.Keys.P256dh.Length > 255 ||
            string.IsNullOrWhiteSpace(request.Keys.Auth) || request.Keys.Auth.Length > 255)
            return BadRequest("Faltan las claves de la suscripción.");

        await _repository.GuardarAsync(new PushSuscripcion
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            Endpoint = request.Endpoint,
            P256dh = request.Keys.P256dh,
            Auth = request.Keys.Auth,
            FechaCreacion = DateTime.UtcNow
        });

        return Ok();
    }

    [HttpPost("suscripciones/eliminar")]
    public async Task<IActionResult> Desuscribir(EliminarSuscripcionPushRequest request)
    {
        await _repository.EliminarAsync(_tenantProvider.TenantId, request.Endpoint);
        return Ok();
    }

    [HttpPost("prueba")]
    public async Task<IActionResult> Probar()
    {
        var enviados = await _notificador.EnviarAsync(
            _tenantProvider.TenantId,
            new NotificacionPush(
                "Notificaciones activadas ✅",
                "Así te vamos a avisar cuando entre una reserva nueva.",
                "/admin",
                "prueba"),
            HttpContext.RequestAborted);

        return Ok(new { enviados });
    }
}

public class SuscripcionPushRequest
{
    public string Endpoint { get; set; } = string.Empty;

    public SuscripcionPushKeys? Keys { get; set; }
}

public class SuscripcionPushKeys
{
    public string P256dh { get; set; } = string.Empty;

    public string Auth { get; set; } = string.Empty;
}

public class EliminarSuscripcionPushRequest
{
    public string Endpoint { get; set; } = string.Empty;
}
