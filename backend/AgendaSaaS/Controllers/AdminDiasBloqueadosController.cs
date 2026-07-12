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

    public AdminDiasBloqueadosController(
        ITenantProvider tenantProvider,
        IDiasBloqueadosRepository repository)
    {
        _tenantProvider = tenantProvider;
        _repository = repository;
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
        if (request.Fecha < DateTime.Today)
            return BadRequest("No podés bloquear fechas pasadas.");

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
            Motivo = request.Motivo
        };

        await _repository.CrearAsync(dia);
        return Ok(new { dia.Id, Fecha = dia.Fecha.ToString("yyyy-MM-dd"), dia.Motivo });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desbloquear(Guid id)
    {
        await _repository.EliminarAsync(id);
        return Ok();
    }
}

public class BloquearDiaRequest
{
    public DateTime Fecha { get; set; }
    public string? Motivo { get; set; }
}
