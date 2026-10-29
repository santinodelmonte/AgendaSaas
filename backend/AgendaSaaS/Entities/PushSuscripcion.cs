namespace AgendaSaaS.Entities;

/// <summary>
/// Un dispositivo (navegador / app instalada) de una manicurista suscripto a
/// notificaciones push.
/// </summary>
public class PushSuscripcion
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string P256dh { get; set; } = string.Empty;

    public string Auth { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
}
