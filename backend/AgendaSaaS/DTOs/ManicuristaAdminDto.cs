namespace AgendaSaaS.DTOs;

public class ManicuristaAdminDto
{
    public Guid TenantId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string WhatsApp { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public int DuracionTurnoMinutos { get; set; }

    public string? LoginEmail { get; set; }

    public bool UsuarioActivo { get; set; }

    public int CantidadTurnos { get; set; }
}
