using AgendaSaaS.DTOs;
using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/turnos")]
public class AdminTurnosController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITurnoRepository _turnoRepository;

    public AdminTurnosController(
        ITenantProvider tenantProvider,
        ITurnoRepository turnoRepository)
    {
        _tenantProvider = tenantProvider;
        _turnoRepository = turnoRepository;
    }

    [HttpGet("pendientes")]
    public async Task<IActionResult> Pendientes()
    {
        var tenantId =
            _tenantProvider.TenantId;

        var resultado =
            await _turnoRepository
                .ObtenerPendientesAsync(
                    tenantId);

        return Ok(resultado);
    }

    [HttpPost("confirmar/{id}")]
    public async Task<IActionResult> Confirmar(
        Guid id)
    {
        var tenantId =
            _tenantProvider.TenantId;

        var turno =
            await _turnoRepository
                .ObtenerPorIdAsync(id);

        if (turno == null || turno.TenantId != tenantId)
            return NotFound();

        if (turno.Estado != TurnoEstado.Pendiente)
            return BadRequest(
                "El turno no está pendiente.");

        turno.Estado =
            TurnoEstado.Confirmado;

        await _turnoRepository
            .ActualizarAsync(turno);

        return Ok(new
        {
            turno.Id,
            turno.FechaHora,
            turno.NombreCliente,
            turno.TelefonoCliente,
            turno.ServicioSolicitado
        });
    }

    [HttpPost("rechazar/{id}")]
    public async Task<IActionResult> Rechazar(
        Guid id)
    {
        var tenantId =
            _tenantProvider.TenantId;

        var turno =
            await _turnoRepository
                .ObtenerPorIdAsync(id);

        if (turno == null || turno.TenantId != tenantId)
            return NotFound();

        if (turno.Estado != TurnoEstado.Pendiente)
            return BadRequest(
                "El turno no está pendiente.");

        turno.Estado =
            TurnoEstado.Disponible;

        turno.NombreCliente = null;
        turno.TelefonoCliente = null;
        turno.ServicioSolicitado = null;
        turno.FechaCreacionPendiente = null;

        await _turnoRepository
            .ActualizarAsync(turno);

        return Ok();
    }

    [HttpPost("cancelar/{id}")]
    public async Task<IActionResult> Cancelar(
        Guid id)
    {
        var tenantId =
            _tenantProvider.TenantId;

        var turno =
            await _turnoRepository
                .ObtenerPorIdAsync(id);

        if (turno == null || turno.TenantId != tenantId)
            return NotFound();

        turno.Estado =
            TurnoEstado.Disponible;

        turno.NombreCliente = null;
        turno.TelefonoCliente = null;
        turno.ServicioSolicitado = null;
        turno.FechaCreacionPendiente = null;

        await _turnoRepository
            .ActualizarAsync(turno);

        return Ok();
    }

    [HttpPost("crear-manual")]
    public async Task<IActionResult> CrearManual(
        CrearTurnoManualRequest request)
    {
        var turno =
            new Turno
            {
                Id = Guid.NewGuid(),
                TenantId =
                    _tenantProvider.TenantId,
                FechaHora =
                    request.FechaHora,
                NotaInterna =
                    request.Nota,
                Estado =
                    TurnoEstado.Confirmado,
                Version = 1
            };

        await _turnoRepository
            .CrearAsync(turno);

        return Ok(turno);
    }
}