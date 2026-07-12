using AgendaSaaS.DTOs;
using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/turnos")]
public class TurnosController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ITurnoRepository _turnoRepository;
    private readonly IBloqueRepository _bloqueRepository;
    private readonly IManicuristaRepository _manicuristaRepository;
    private readonly IDiasBloqueadosRepository _diasBloqueadosRepository;

    public TurnosController(
        ITenantProvider tenantProvider,
        ITurnoRepository turnoRepository,
        IBloqueRepository bloqueRepository,
        IManicuristaRepository manicuristaRepository,
        IDiasBloqueadosRepository diasBloqueadosRepository)
    {
        _tenantProvider = tenantProvider;
        _turnoRepository = turnoRepository;
        _bloqueRepository = bloqueRepository;
        _manicuristaRepository = manicuristaRepository;
        _diasBloqueadosRepository = diasBloqueadosRepository;
    }

    [AllowAnonymous]
    [HttpGet("disponibles")]
    public async Task<IActionResult> ObtenerDisponibles(
        [FromQuery] DateTime fecha)
    {
        var tenantId = _tenantProvider.TenantId;

        var manicurista =
            await _manicuristaRepository
                .ObtenerPorTenantAsync(tenantId);

        if (manicurista is null)
            return NotFound("Manicurista no encontrada.");

        var diaBloqueado = await _diasBloqueadosRepository
            .ObtenerPorFechaAsync(tenantId, fecha);

        if (diaBloqueado != null)
            return Ok(new List<TurnoDisponibleDto>());

        var bloques =
            await _bloqueRepository
                .ObtenerPorTenantAsync(tenantId);

        var bloque =
            bloques
                .Where(x => x.DiaSemana == fecha.DayOfWeek)
                .OrderBy(x => x.PausaInicio.HasValue ? 0 : 1)
                .ThenBy(x => x.HoraInicio)
                .FirstOrDefault();

        if (bloque is null)
            return Ok(new List<TurnoDisponibleDto>());

        var turnosExistentes =
            await _turnoRepository
                .ObtenerPorFechaAsync(
                    tenantId,
                    fecha);

        var resultado =
            new List<TurnoDisponibleDto>();

        var actual =
            fecha.Date + bloque.HoraInicio;

        var fin =
            fecha.Date + bloque.HoraFin;

        while (actual < fin)
        {
            var horaActual = actual.TimeOfDay;

            var estaEnPausa =
                bloque.PausaInicio.HasValue &&
                bloque.PausaFin.HasValue &&
                horaActual >= bloque.PausaInicio.Value &&
                horaActual < bloque.PausaFin.Value;

            if (!estaEnPausa)
            {
                // Los turnos gestionados desde el admin pueden estar corridos de la
                // grilla o durar más que un slot: se compara por solapamiento para
                // no ofrecer horarios que en realidad están ocupados.
                var turno =
                    turnosExistentes
                        .FirstOrDefault(t =>
                            t.Estado is TurnoEstado.Pendiente or TurnoEstado.Confirmado
                                ? actual < t.FechaHora.AddMinutes(t.DuracionMinutos ?? manicurista.DuracionTurnoMinutos)
                                    && t.FechaHora < actual.AddMinutes(manicurista.DuracionTurnoMinutos)
                                : t.FechaHora == actual);

                resultado.Add(
                    new TurnoDisponibleDto
                    {
                        FechaHora = actual,
                        Estado = turno == null
                            ? TurnoEstado.Disponible.ToString()
                            : turno.Estado.ToString()
                    });
            }

            actual =
                actual.AddMinutes(
                    manicurista.DuracionTurnoMinutos);
        }

        return Ok(resultado);
    }

    [AllowAnonymous]
    [HttpPost("solicitar")]
    public async Task<IActionResult> Solicitar(
        SolicitarTurnoRequest request)
    {
        var tenantId =
            _tenantProvider.TenantId;

        if (string.IsNullOrWhiteSpace(request.NombreCliente) || request.NombreCliente.Length > 100)
            return BadRequest("El nombre debe tener entre 1 y 100 caracteres.");

        if (string.IsNullOrWhiteSpace(request.TelefonoCliente) || request.TelefonoCliente.Length > 20)
            return BadRequest("El teléfono debe tener entre 1 y 20 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Servicio) || request.Servicio.Length > 200)
            return BadRequest("El servicio debe tener entre 1 y 200 caracteres.");

        if (request.FechaHora <= DateTime.Now)
            return BadRequest("No se puede reservar un turno en el pasado.");

        if (request.FechaHora <= DateTime.Now.AddHours(2))
            return BadRequest("Debés reservar con al menos 2 horas de anticipación.");

        var diaBloqueadoSolicitar = await _diasBloqueadosRepository
            .ObtenerPorFechaAsync(tenantId, request.FechaHora);

        if (diaBloqueadoSolicitar != null)
            return BadRequest("Ese día está bloqueado y no admite reservas.");

        var manicurista =
            await _manicuristaRepository
                .ObtenerPorTenantAsync(tenantId);

        if (manicurista is null)
            return NotFound("Manicurista no encontrada.");

        var bloques =
            await _bloqueRepository
                .ObtenerPorTenantAsync(tenantId);

        var bloque =
            bloques
                .Where(x => x.DiaSemana == request.FechaHora.DayOfWeek)
                .OrderBy(x => x.HoraInicio)
                .FirstOrDefault();

        if (bloque is null)
        {
            return BadRequest(
                "No hay horario configurado para ese día.");
        }

        if (!EsHorarioReservable(
                request.FechaHora,
                bloque,
                manicurista.DuracionTurnoMinutos))
        {
            return BadRequest(
                "La hora solicitada no está disponible.");
        }

        var turnosDia =
            await _turnoRepository
                .ObtenerPorFechaAsync(
                    tenantId,
                    request.FechaHora.Date);

        var hayOcupadoSolapado =
            turnosDia.Any(t =>
                t.Estado is TurnoEstado.Pendiente or TurnoEstado.Confirmado &&
                request.FechaHora < t.FechaHora.AddMinutes(t.DuracionMinutos ?? manicurista.DuracionTurnoMinutos) &&
                t.FechaHora < request.FechaHora.AddMinutes(manicurista.DuracionTurnoMinutos));

        if (hayOcupadoSolapado)
            return BadRequest("El turno ya no está disponible.");

        var turno =
            turnosDia.FirstOrDefault(
                t => t.FechaHora == request.FechaHora);

        if (turno != null)
        {
            if (turno.Estado != TurnoEstado.Disponible)
            {
                return BadRequest(
                    "El turno ya no está disponible.");
            }

            turno.Estado =
                TurnoEstado.Pendiente;

            turno.NombreCliente =
                request.NombreCliente;

            turno.TelefonoCliente =
                request.TelefonoCliente;

            turno.ServicioSolicitado =
                request.Servicio;

            turno.FechaCreacionPendiente =
                DateTime.UtcNow;

            await _turnoRepository
                .ActualizarAsync(turno);

            return Ok();
        }

        var nuevoTurno =
            new Turno
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FechaHora = request.FechaHora,
                NombreCliente = request.NombreCliente,
                TelefonoCliente = request.TelefonoCliente,
                ServicioSolicitado = request.Servicio,
                Estado = TurnoEstado.Pendiente,
                FechaCreacionPendiente = DateTime.UtcNow,
                Version = 1
            };

        await _turnoRepository
            .CrearAsync(nuevoTurno);

        return Ok();
    }

    private static bool EsHorarioReservable(
        DateTime fechaHora,
        BloqueHorarioConfig bloque,
        int duracionTurnoMinutos)
    {
        var actual =
            fechaHora.Date + bloque.HoraInicio;

        var fin =
            fechaHora.Date + bloque.HoraFin;

        while (actual < fin)
        {
            var horaActual = actual.TimeOfDay;

            var estaEnPausa =
                bloque.PausaInicio.HasValue &&
                bloque.PausaFin.HasValue &&
                horaActual >= bloque.PausaInicio.Value &&
                horaActual < bloque.PausaFin.Value;

            if (!estaEnPausa && actual == fechaHora)
                return true;

            actual =
                actual.AddMinutes(duracionTurnoMinutos);
        }

        return false;
    }
}