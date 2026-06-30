using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface IBloqueRepository
{
    Task<List<BloqueHorarioConfig>>
        ObtenerPorTenantAsync(Guid tenantId);

    Task<BloqueHorarioConfig?>
        ObtenerPorIdAsync(Guid id);

    Task CrearAsync(
        BloqueHorarioConfig bloque);

    Task ActualizarAsync(
        BloqueHorarioConfig bloque);

    Task EliminarAsync(
        Guid id);
}