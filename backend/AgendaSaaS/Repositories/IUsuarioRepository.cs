using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(
        string email);

    Task CrearAsync(
    Usuario usuario);
}