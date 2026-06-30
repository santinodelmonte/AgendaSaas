using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface ITurnoRepository
{
    Task<List<Turno>> ObtenerPorFechaAsync(
        Guid tenantId,
        DateTime fecha);

    Task<Turno?> ObtenerPorFechaHoraAsync(
        Guid tenantId,
        DateTime fechaHora);

    Task<Turno?> ObtenerPorIdAsync(
        Guid id);

    Task<List<Turno>> ObtenerPendientesAsync(
        Guid tenantId);

    Task<int> ContarPendientesAsync(
        Guid tenantId);

    Task<int> ContarConfirmadosEntreFechasAsync(
        Guid tenantId,
        DateTime inicio,
        DateTime fin);

    Task<Turno?> ObtenerProximoConfirmadoAsync(
        Guid tenantId,
        DateTime desde);

    Task CrearAsync(
        Turno turno);

    Task ActualizarAsync(
        Turno turno);
}