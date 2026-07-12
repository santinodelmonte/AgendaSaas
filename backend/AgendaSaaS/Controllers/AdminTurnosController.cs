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
    private readonly IManicuristaRepository _manicuristaRepository;

    public AdminTurnosController(
        ITenantProvider tenantProvider,
        ITurnoRepository turnoRepository,
        IManicuristaRepository manicuristaRepository)
    {
        _tenantProvider = tenantProvider;
        _turnoRepository = turnoRepository;
        _manicuristaRepository = manicuristaRepository;
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

        LiberarTurno(turno);

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

        if (turno.FechaHora < DateTime.Now)
            return BadRequest("No se puede cancelar un turno que ya pasó.");

        LiberarTurno(turno);

        await _turnoRepository
            .ActualizarAsync(turno);

        return Ok();
    }

    [HttpPost("crear-manual")]
    public async Task<IActionResult> CrearManual(
        CrearTurnoManualRequest request)
    {
        var tenantId = _tenantProvider.TenantId;

        if (request.FechaHora <= DateTime.Now)
            return BadRequest("No se puede crear un turno en el pasado.");

        if (request.DuracionMinutos is < 15 or > 480)
            return BadRequest("La duración debe estar entre 15 y 480 minutos.");

        var duracionDefault =
            await ObtenerDuracionDefaultAsync(tenantId);

        var existente =
            await _turnoRepository
                .ObtenerPorFechaHoraAsync(
                    tenantId,
                    request.FechaHora);

        if (existente != null && existente.Estado != TurnoEstado.Disponible)
            return BadRequest("Ese horario ya está ocupado.");

        var conflicto =
            await BuscarSolapamientoAsync(
                tenantId,
                request.FechaHora,
                request.DuracionMinutos ?? duracionDefault,
                duracionDefault,
                excluirId: existente?.Id);

        if (conflicto != null)
            return BadRequest("Ese horario se superpone con otro turno.");

        if (existente != null)
        {
            existente.Estado = TurnoEstado.Confirmado;
            existente.NombreCliente = request.NombreCliente;
            existente.TelefonoCliente = request.TelefonoCliente;
            existente.ServicioSolicitado = request.Servicio;
            existente.NotaInterna = request.Nota;
            existente.DuracionMinutos = request.DuracionMinutos;

            await _turnoRepository.ActualizarAsync(existente);
            return Ok(existente);
        }

        var turno =
            new Turno
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FechaHora = request.FechaHora,
                NombreCliente = request.NombreCliente,
                TelefonoCliente = request.TelefonoCliente,
                ServicioSolicitado = request.Servicio,
                NotaInterna = request.Nota,
                Estado = TurnoEstado.Confirmado,
                DuracionMinutos = request.DuracionMinutos,
                Version = 1
            };

        await _turnoRepository.CrearAsync(turno);
        return Ok(turno);
    }

    [HttpPut("reprogramar/{id}")]
    public async Task<IActionResult> Reprogramar(
        Guid id,
        ReprogramarTurnoRequest request)
    {
        var tenantId = _tenantProvider.TenantId;

        var turno =
            await _turnoRepository
                .ObtenerPorIdAsync(id);

        if (turno == null || turno.TenantId != tenantId)
            return NotFound();

        if (turno.Estado is not (TurnoEstado.Pendiente or TurnoEstado.Confirmado))
            return BadRequest("Solo se pueden reprogramar turnos pendientes o confirmados.");

        // Se permite editar solo la duración de un turno pasado; solo se bloquea
        // MOVER el turno a un horario en el pasado.
        if (request.FechaHora != turno.FechaHora && request.FechaHora <= DateTime.Now)
            return BadRequest("No se puede mover un turno al pasado.");

        if (request.DuracionMinutos is < 15 or > 480)
            return BadRequest("La duración debe estar entre 15 y 480 minutos.");

        var duracionDefault =
            await ObtenerDuracionDefaultAsync(tenantId);

        var duracion =
            request.DuracionMinutos
            ?? turno.DuracionMinutos
            ?? duracionDefault;

        var conflicto =
            await BuscarSolapamientoAsync(
                tenantId,
                request.FechaHora,
                duracion,
                duracionDefault,
                excluirId: turno.Id);

        if (conflicto != null)
            return BadRequest("El nuevo horario se superpone con otro turno.");

        turno.FechaHora = request.FechaHora;
        turno.DuracionMinutos = request.DuracionMinutos ?? turno.DuracionMinutos;

        await _turnoRepository.ActualizarAsync(turno);

        return Ok(new
        {
            turno.Id,
            turno.FechaHora,
            turno.DuracionMinutos
        });
    }

    [HttpPost("intercambiar")]
    public async Task<IActionResult> Intercambiar(
        IntercambiarTurnosRequest request)
    {
        var tenantId = _tenantProvider.TenantId;

        if (request.TurnoAId == request.TurnoBId)
            return BadRequest("Elegí dos turnos distintos.");

        var turnoA = await _turnoRepository.ObtenerPorIdAsync(request.TurnoAId);
        var turnoB = await _turnoRepository.ObtenerPorIdAsync(request.TurnoBId);

        if (turnoA == null || turnoA.TenantId != tenantId ||
            turnoB == null || turnoB.TenantId != tenantId)
            return NotFound();

        if (turnoA.Estado is not (TurnoEstado.Pendiente or TurnoEstado.Confirmado) ||
            turnoB.Estado is not (TurnoEstado.Pendiente or TurnoEstado.Confirmado))
            return BadRequest("Solo se pueden intercambiar turnos pendientes o confirmados.");

        var duracionDefault =
            await ObtenerDuracionDefaultAsync(tenantId);

        // Posición futura de cada turno tras el intercambio
        var nuevaFechaA = turnoB.FechaHora;
        var nuevaFechaB = turnoA.FechaHora;

        var nuevaDuracionA = request.IntercambiarDuraciones ? turnoB.DuracionMinutos : turnoA.DuracionMinutos;
        var nuevaDuracionB = request.IntercambiarDuraciones ? turnoA.DuracionMinutos : turnoB.DuracionMinutos;

        var conflictoA =
            await BuscarSolapamientoAsync(
                tenantId, nuevaFechaA, nuevaDuracionA ?? duracionDefault,
                duracionDefault, excluirId: turnoA.Id, excluirId2: turnoB.Id);

        var conflictoB =
            await BuscarSolapamientoAsync(
                tenantId, nuevaFechaB, nuevaDuracionB ?? duracionDefault,
                duracionDefault, excluirId: turnoA.Id, excluirId2: turnoB.Id);

        if (conflictoA != null || conflictoB != null)
            return BadRequest("El intercambio genera superposición con otro turno.");

        if (!request.IntercambiarDuraciones &&
            SeSuperponen(
                nuevaFechaA, nuevaDuracionA ?? duracionDefault,
                nuevaFechaB, nuevaDuracionB ?? duracionDefault))
            return BadRequest("Los turnos intercambiados se superponen entre sí; probá intercambiando también las duraciones.");

        turnoA.FechaHora = nuevaFechaA;
        turnoB.FechaHora = nuevaFechaB;
        turnoA.DuracionMinutos = nuevaDuracionA;
        turnoB.DuracionMinutos = nuevaDuracionB;

        await _turnoRepository.ActualizarAsync(turnoA);
        await _turnoRepository.ActualizarAsync(turnoB);

        return Ok(new
        {
            TurnoA = new { turnoA.Id, turnoA.FechaHora, turnoA.DuracionMinutos },
            TurnoB = new { turnoB.Id, turnoB.FechaHora, turnoB.DuracionMinutos }
        });
    }

    private static void LiberarTurno(Turno turno)
    {
        turno.Estado = TurnoEstado.Disponible;
        turno.NombreCliente = null;
        turno.TelefonoCliente = null;
        turno.ServicioSolicitado = null;
        turno.FechaCreacionPendiente = null;
        turno.DuracionMinutos = null;
    }

    private async Task<int> ObtenerDuracionDefaultAsync(
        Guid tenantId)
    {
        var manicurista =
            await _manicuristaRepository
                .ObtenerPorTenantAsync(tenantId);

        return manicurista?.DuracionTurnoMinutos ?? 30;
    }

    private async Task<Turno?> BuscarSolapamientoAsync(
        Guid tenantId,
        DateTime fechaHora,
        int duracionMinutos,
        int duracionDefault,
        Guid? excluirId = null,
        Guid? excluirId2 = null)
    {
        var turnosDia =
            await _turnoRepository
                .ObtenerPorFechaAsync(tenantId, fechaHora.Date);

        return turnosDia.FirstOrDefault(t =>
            t.Id != excluirId &&
            t.Id != excluirId2 &&
            (t.Estado is TurnoEstado.Pendiente or TurnoEstado.Confirmado) &&
            SeSuperponen(
                fechaHora,
                duracionMinutos,
                t.FechaHora,
                t.DuracionMinutos ?? duracionDefault));
    }

    private static bool SeSuperponen(
        DateTime inicioA,
        int duracionA,
        DateTime inicioB,
        int duracionB)
    {
        return inicioA < inicioB.AddMinutes(duracionB)
            && inicioB < inicioA.AddMinutes(duracionA);
    }
}
