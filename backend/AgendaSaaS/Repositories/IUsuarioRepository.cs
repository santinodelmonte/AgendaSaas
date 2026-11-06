using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(
        string email);

    /// <summary>
    /// Versión de token vigente del usuario, o null si no existe o está inactivo.
    /// </summary>
    Task<int?> ObtenerTokenVersionSiActivoAsync(
        Guid id);

    Task CrearAsync(
    Usuario usuario);
}