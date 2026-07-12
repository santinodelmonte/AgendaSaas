using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface IDiasBloqueadosRepository
{
    Task<List<DiasBloqueado>> ObtenerPorTenantAsync(Guid tenantId);

    Task<DiasBloqueado?> ObtenerPorFechaAsync(Guid tenantId, DateTime fecha);

    Task CrearAsync(DiasBloqueado dia);

    Task EliminarAsync(Guid id);
}
