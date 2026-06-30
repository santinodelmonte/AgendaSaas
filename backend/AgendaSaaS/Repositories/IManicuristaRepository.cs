using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface IManicuristaRepository
{
    Task<Manicurista?> ObtenerPorTenantAsync(
        Guid tenantId);

    Task ActualizarAsync(
        Manicurista manicurista);
    Task<Manicurista?> ObtenerPorSlugAsync(
    string slug);
}