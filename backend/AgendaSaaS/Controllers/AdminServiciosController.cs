using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/servicios")]
public class AdminServiciosController : ControllerBase
{
    private readonly IServicioRepository _repo;
    private readonly ITenantProvider _tenant;

    public AdminServiciosController(IServicioRepository repo, ITenantProvider tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var tenantId = _tenant.TenantId;
        var servicios = await _repo.ObtenerPorTenantAsync(tenantId);
        return Ok(servicios.Select(s => new
        {
            s.Id,
            s.Nombre,
            s.Precio,
            s.Activo,
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ServicioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Length > 200)
            return BadRequest("El nombre debe tener entre 1 y 200 caracteres.");

        if (request.Precio < 0)
            return BadRequest("El precio no puede ser negativo.");

        var servicio = new Servicio
        {
            TenantId = _tenant.TenantId,
            Nombre = request.Nombre.Trim(),
            Precio = request.Precio,
            Activo = true,
        };

        var creado = await _repo.CrearAsync(servicio);

        return Ok(new { creado.Id, creado.Nombre, creado.Precio, creado.Activo });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ServicioRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Length > 200)
            return BadRequest("El nombre debe tener entre 1 y 200 caracteres.");

        if (request.Precio < 0)
            return BadRequest("El precio no puede ser negativo.");

        var servicio = new Servicio
        {
            Id = id,
            TenantId = _tenant.TenantId,
            Nombre = request.Nombre.Trim(),
            Precio = request.Precio,
            Activo = request.Activo,
        };

        var actualizado = await _repo.ActualizarAsync(servicio);

        if (actualizado is null)
            return NotFound();

        return Ok(new { actualizado.Id, actualizado.Nombre, actualizado.Precio, actualizado.Activo });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var eliminado = await _repo.EliminarAsync(id, _tenant.TenantId);

        if (!eliminado)
            return NotFound();

        return NoContent();
    }
}

public record ServicioRequest(string Nombre, decimal Precio, bool Activo = true);
