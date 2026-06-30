namespace AgendaSaaS.Entities;

public class Turno
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public DateTime FechaHora { get; set; }

    public string? NombreCliente { get; set; }

    public string? TelefonoCliente { get; set; }

    public string? ServicioSolicitado { get; set; }

    public string? NotaInterna { get; set; }

    public TurnoEstado Estado { get; set; }

    public DateTime? FechaCreacionPendiente { get; set; }

    public long Version { get; set; }
}