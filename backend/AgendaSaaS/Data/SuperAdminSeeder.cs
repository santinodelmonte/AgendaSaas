using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;

namespace AgendaSaaS.Data;

public static class SuperAdminSeeder
{
    /// <summary>
    /// Crea el usuario superadmin a partir de la configuración (SuperAdmin:Email /
    /// SuperAdmin:Password) si todavía no existe. Evita depender de endpoints
    /// abiertos: el superadmin se aprovisiona solo desde el entorno.
    /// </summary>
    public static async Task SembrarAsync(
        IUsuarioRepository usuarioRepository,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "SuperAdmin:Email/Password no configurados; no se sembró el superadmin.");
            return;
        }

        var existente =
            await usuarioRepository.ObtenerPorEmailAsync(email);

        if (existente != null)
            return;

        var superadmin =
            new Usuario
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.Empty,
                Email = email,
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(password),
                Activo = true,
                Rol = UsuarioRol.SuperAdmin
            };

        await usuarioRepository.CrearAsync(superadmin);

        logger.LogInformation(
            "Superadmin creado para {Email}.", email);
    }
}
