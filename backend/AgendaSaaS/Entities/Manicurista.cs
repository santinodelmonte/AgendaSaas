namespace AgendaSaaS.Entities;

public class Manicurista
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public string WhatsApp { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public int DuracionTurnoMinutos { get; set; }
}