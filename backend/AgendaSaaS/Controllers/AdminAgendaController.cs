using AgendaSaaS.Entities;
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
    private readonly IBloqueRepository _bloqueRepository;
    private readonly IManicuristaRepository _manicuristaRepository;
    private readonly IDiasBloqueadosRepository _diasBloqueadosRepository;

    public AdminAgendaController(
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
                    t.NotaInterna,
                    t.DuracionMinutos
                });

        return Ok(resultado);
    }

    [HttpGet("historial")]
    public async Task<IActionResult> ObtenerHistorial(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20)
    {
        var tenantId = _tenantProvider.TenantId;

        var turnos = await _turnoRepository
            .ObtenerHistorialAsync(tenantId, pagina, tamano);

        var resultado = turnos.Select(t => new
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

    [HttpGet("slots")]
    public async Task<IActionResult> ObtenerSlots(
        [FromQuery] DateTime fecha)
    {
        var tenantId = _tenantProvider.TenantId;

        var diaBloqueado = await _diasBloqueadosRepository
            .ObtenerPorFechaAsync(tenantId, fecha);

        if (diaBloqueado != null)
            return Ok(new { bloqueado = true, motivo = diaBloqueado.Motivo, slots = new List<object>() });

        var manicurista =
            await _manicuristaRepository
                .ObtenerPorTenantAsync(tenantId);

        if (manicurista is null)
            return NotFound("Manicurista no encontrada.");

        var bloques =
            await _bloqueRepository
                .ObtenerPorTenantAsync(tenantId);

        var bloque =
            bloques.FirstOrDefault(
                x => x.DiaSemana == fecha.DayOfWeek);

        if (bloque is null)
            return Ok(new List<object>());

        var turnos =
            await _turnoRepository
                .ObtenerPorFechaAsync(tenantId, fecha);

        var resultado = new List<object>();
        var actual = fecha.Date + bloque.HoraInicio;
        var fin = fecha.Date + bloque.HoraFin;

        while (actual < fin)
        {
            var hora = actual.TimeOfDay;

            var enPausa =
                bloque.PausaInicio.HasValue &&
                bloque.PausaFin.HasValue &&
                hora >= bloque.PausaInicio.Value &&
                hora < bloque.PausaFin.Value;

            if (!enPausa)
            {
                // Los turnos ocupados pueden estar corridos de la grilla o durar más
                // que un slot (reprogramación manual), por eso se compara por solapamiento.
                var turno = turnos.FirstOrDefault(t =>
                    t.Estado is TurnoEstado.Pendiente or TurnoEstado.Confirmado
                        ? actual < t.FechaHora.AddMinutes(t.DuracionMinutos ?? manicurista.DuracionTurnoMinutos)
                            && t.FechaHora < actual.AddMinutes(manicurista.DuracionTurnoMinutos)
                        : t.FechaHora == actual);

                resultado.Add(new
                {
                    FechaHora = actual,
                    Estado = turno?.Estado.ToString()
                        ?? TurnoEstado.Disponible.ToString(),
                    Id = turno?.Id,
                    NombreCliente = turno?.NombreCliente,
                    TelefonoCliente = turno?.TelefonoCliente,
                    ServicioSolicitado = turno?.ServicioSolicitado,
                    NotaInterna = turno?.NotaInterna,
                    DuracionMinutos = turno?.DuracionMinutos
                });
            }

            actual = actual.AddMinutes(manicurista.DuracionTurnoMinutos);
        }

        return Ok(new { bloqueado = false, motivo = (string?)null, slots = resultado });
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
                        t.NotaInterna,
                        t.DuracionMinutos
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