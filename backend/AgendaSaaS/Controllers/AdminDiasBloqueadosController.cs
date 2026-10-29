using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/dias-bloqueados")]
public class AdminDiasBloqueadosController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IDiasBloqueadosRepository _repository;
    private readonly IClock _clock;

    public AdminDiasBloqueadosController(
        ITenantProvider tenantProvider,
        IDiasBloqueadosRepository repository,
        IClock clock)
    {
        _tenantProvider = tenantProvider;
        _repository = repository;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var dias = await _repository.ObtenerPorTenantAsync(_tenantProvider.TenantId);
        return Ok(dias.Select(d => new
        {
            d.Id,
            Fecha = d.Fecha.ToString("yyyy-MM-dd"),
            d.Motivo
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Bloquear([FromBody] BloquearDiaRequest request)
    {
        if (request.Fecha.Date < _clock.Today)
            return BadRequest("No podés bloquear fechas pasadas.");

        if (request.Motivo?.Length > 200)
            return BadRequest("El motivo no puede superar los 200 caracteres.");

        var existente = await _repository.ObtenerPorFechaAsync(
            _tenantProvider.TenantId,
            request.Fecha);

        if (existente != null)
            return BadRequest("Esa fecha ya está bloqueada.");

        var dia = new DiasBloqueado
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.TenantId,
            Fecha = request.Fecha.Date,
            Motivo = string.IsNullOrWhiteSpace(request.Motivo) ? null : request.Motivo.Trim()
        };

        await _repository.CrearAsync(dia);
        return Ok(new { dia.Id, Fecha = dia.Fecha.ToString("yyyy-MM-dd"), dia.Motivo });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desbloquear(Guid id)
    {
        var eliminado = await _repository.EliminarAsync(id, _tenantProvider.TenantId);

        if (!eliminado)
            return NotFound();

        return Ok();
    }
}

public class BloquearDiaRequest
{
    public DateTime Fecha { get; set; }
    public string? Motivo { get; set; }
}
