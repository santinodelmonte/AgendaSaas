using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface IPushSuscripcionRepository
{
    Task GuardarAsync(PushSuscripcion suscripcion);

    Task<List<PushSuscripcion>> ObtenerPorTenantAsync(Guid tenantId);

    Task<bool> EliminarAsync(Guid tenantId, string endpoint);

    Task EliminarPorIdAsync(Guid id);
}
