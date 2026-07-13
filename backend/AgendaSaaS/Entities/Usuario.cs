namespace AgendaSaaS.Entities;

public class Usuario
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public UsuarioRol Rol { get; set; }
}