namespace AgendaSaaS.Entities;

public class DiasBloqueado
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public DateTime Fecha { get; set; }

    public string? Motivo { get; set; }
}
