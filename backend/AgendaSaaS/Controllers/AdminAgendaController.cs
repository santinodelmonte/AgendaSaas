using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/agenda")]
public class AdminAgendaController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITurnoRepository _turnoRepository;

    public AdminAgendaController(
        ITenantProvider tenantProvider,
        ITurnoRepository turnoRepository)
    {
        _tenantProvider = tenantProvider;
        _turnoRepository = turnoRepository;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerDia(
        [FromQuery] DateTime fecha)
    {
        var tenantId = _tenantProvider.TenantId;

        var turnos =
            await _turnoRepository
                .ObtenerPorFechaAsync(
                    tenantId,
                    fecha);

        var resultado =
            turnos
                .OrderBy(x => x.FechaHora)
                .Select(t => new
                {
                    t.Id,
                    t.FechaHora,
                    Estado = t.Estado.ToString(),
                    t.NombreCliente,
                    t.TelefonoCliente,
                    t.ServicioSolicitado,
                    t.NotaInterna
                });

        return Ok(resultado);
    }

    [HttpGet("semana")]
    public async Task<IActionResult> ObtenerSemana(
        [FromQuery] DateTime fechaInicio)
    {
        var tenantId = _tenantProvider.TenantId;
        var inicio = fechaInicio.Date;

        var semana = new List<object>();

        for (var i = 0; i < 7; i++)
        {
            var fecha = inicio.AddDays(i);

            var turnos =
                await _turnoRepository
                    .ObtenerPorFechaAsync(
                        tenantId,
                        fecha);

            var turnosDia =
                turnos
                    .OrderBy(x => x.FechaHora)
                    .Select(t => new
                    {
                        t.Id,
                        t.FechaHora,
                        Estado = t.Estado.ToString(),
                        t.NombreCliente,
                        t.TelefonoCliente,
                        t.ServicioSolicitado,
                        t.NotaInterna
                    })
                    .ToList();

            semana.Add(new
            {
                Fecha = fecha,
                Turnos = turnosDia
            });
        }

        return Ok(semana);
    }
}