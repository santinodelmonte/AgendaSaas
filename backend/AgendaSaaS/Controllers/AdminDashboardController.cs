using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITurnoRepository _turnoRepository;

    public AdminDashboardController(
        ITenantProvider tenantProvider,
        ITurnoRepository turnoRepository)
    {
        _tenantProvider = tenantProvider;
        _turnoRepository = turnoRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var tenantId = _tenantProvider.TenantId;
        var ahora = DateTime.Now;

        var inicioHoy = ahora.Date;
        var finHoy = inicioHoy.AddDays(1);

        var inicioMes =
            new DateTime(
                ahora.Year,
                ahora.Month,
                1);

        var finMes =
            inicioMes.AddMonths(1);

        var pendientes =
            await _turnoRepository
                .ContarPendientesAsync(tenantId);

        var confirmadosHoy =
            await _turnoRepository
                .ContarConfirmadosEntreFechasAsync(
                    tenantId,
                    inicioHoy,
                    finHoy);

        var confirmadosMes =
            await _turnoRepository
                .ContarConfirmadosEntreFechasAsync(
                    tenantId,
                    inicioMes,
                    finMes);

        var proximoTurno =
            await _turnoRepository
                .ObtenerProximoConfirmadoAsync(
                    tenantId,
                    ahora);

        return Ok(new
        {
            turnosPendientes = pendientes,
            confirmadosHoy = confirmadosHoy,
            confirmadosMes = confirmadosMes,
            proximoTurno = proximoTurno is null
                ? null
                : new
                {
                    proximoTurno.Id,
                    proximoTurno.FechaHora,
                    proximoTurno.NombreCliente,
                    proximoTurno.TelefonoCliente,
                    proximoTurno.ServicioSolicitado,
                    Estado = proximoTurno.Estado.ToString()
                }
        });
    }
}