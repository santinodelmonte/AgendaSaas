using AgendaSaaS.DTOs;
using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/horarios")]
public class AdminHorariosController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IBloqueRepository _repository;

    public AdminHorariosController(
        ITenantProvider tenantProvider,
        IBloqueRepository repository)
    {
        _tenantProvider = tenantProvider;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var bloques =
            await _repository.ObtenerPorTenantAsync(
                _tenantProvider.TenantId);

        return Ok(bloques);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        CrearBloqueHorarioRequest request)
    {
        var validacion =
            ValidarHorario(
                request.HoraInicio,
                request.HoraFin,
                request.PausaInicio,
                request.PausaFin);

        if (validacion != null)
            return BadRequest(validacion);

        var bloques =
            await _repository.ObtenerPorTenantAsync(
                _tenantProvider.TenantId);

        if (bloques.Any(x => x.DiaSemana == request.DiaSemana))
        {
            return BadRequest(
                "Ya existe un horario para ese día.");
        }

        var bloque =
            new BloqueHorarioConfig
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantProvider.TenantId,
                DiaSemana = request.DiaSemana,
                HoraInicio = request.HoraInicio,
                HoraFin = request.HoraFin,
                PausaInicio = request.PausaInicio,
                PausaFin = request.PausaFin
            };

        await _repository.CrearAsync(bloque);

        return Ok(bloque);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(
        Guid id,
        ActualizarBloqueHorarioRequest request)
    {
        var bloque =
            await _repository.ObtenerPorIdAsync(id);

        if (bloque is null)
            return NotFound();

        if (bloque.TenantId != _tenantProvider.TenantId)
            return NotFound();

        var validacion =
            ValidarHorario(
                request.HoraInicio,
                request.HoraFin,
                request.PausaInicio,
                request.PausaFin);

        if (validacion != null)
            return BadRequest(validacion);

        var bloques =
            await _repository.ObtenerPorTenantAsync(
                _tenantProvider.TenantId);

        if (bloques.Any(x =>
                x.DiaSemana == request.DiaSemana &&
                x.Id != id))
        {
            return BadRequest(
                "Ya existe un horario para ese día.");
        }

        bloque.DiaSemana = request.DiaSemana;
        bloque.HoraInicio = request.HoraInicio;
        bloque.HoraFin = request.HoraFin;
        bloque.PausaInicio = request.PausaInicio;
        bloque.PausaFin = request.PausaFin;

        await _repository.ActualizarAsync(bloque);

        return Ok(bloque);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(
        Guid id)
    {
        var bloque =
            await _repository.ObtenerPorIdAsync(id);

        if (bloque is null)
            return NotFound();

        if (bloque.TenantId != _tenantProvider.TenantId)
            return NotFound();

        await _repository.EliminarAsync(id);

        return Ok();
    }

    private static string? ValidarHorario(
        TimeSpan horaInicio,
        TimeSpan horaFin,
        TimeSpan? pausaInicio,
        TimeSpan? pausaFin)
    {
        if (horaInicio >= horaFin)
            return "HoraInicio debe ser menor que HoraFin.";

        var pausaInicioTieneValor = pausaInicio.HasValue;
        var pausaFinTieneValor = pausaFin.HasValue;

        if (pausaInicioTieneValor != pausaFinTieneValor)
        {
            return "PausaInicio y PausaFin deben informarse juntas.";
        }

        if (pausaInicioTieneValor && pausaFinTieneValor)
        {
            if (pausaInicio.Value >= pausaFin.Value)
            {
                return "PausaInicio debe ser menor que PausaFin.";
            }

            if (pausaInicio.Value <= horaInicio ||
                pausaFin.Value >= horaFin)
            {
                return "La pausa debe estar dentro del horario laboral.";
            }
        }

        return null;
    }
}