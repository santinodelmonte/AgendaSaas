namespace AgendaSaaS.DTOs;

public class CrearAdminRequest
{
    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}