using AgendaSaaS.DTOs;
using AgendaSaaS.Entities;

namespace AgendaSaaS.Repositories;

public interface ISuperAdminRepository
{
    Task<List<ManicuristaAdminDto>> ListarAsync();

    Task CrearAsync(
        Manicurista manicurista,
        Usuario usuario,
        List<BloqueHorarioConfig> horarios);

    Task ActualizarAsync(
        Guid tenantId,
        string nombre,
        string slug,
        string whatsApp,
        int duracionTurnoMinutos);

    Task SetActivoAsync(
        Guid tenantId,
        bool activo);

    Task ResetPasswordAsync(
        Guid tenantId,
        string passwordHash);

    Task EliminarAsync(
        Guid tenantId);
}
