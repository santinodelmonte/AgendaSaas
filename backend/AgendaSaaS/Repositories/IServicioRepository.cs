using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface IServicioRepository
{
    Task<IEnumerable<Servicio>> ObtenerPorTenantAsync(Guid tenantId);
    Task<IEnumerable<Servicio>> ObtenerActivosPorTenantAsync(Guid tenantId);
    Task<Servicio> CrearAsync(Servicio servicio);
    Task<Servicio?> ActualizarAsync(Servicio servicio);
    Task<bool> EliminarAsync(Guid id, Guid tenantId);
}
