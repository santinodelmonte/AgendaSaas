using AgendaSaaS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IManicuristaRepository _manicuristas;
    private readonly IServicioRepository _servicios;

    public PublicController(
        IManicuristaRepository manicuristas,
        IServicioRepository servicios)
    {
        _manicuristas = manicuristas;
        _servicios = servicios;
    }

    [HttpGet("perfil/{slug}")]
    public async Task<IActionResult> ObtenerPerfil(string slug)
    {
        var manicurista = await _manicuristas.ObtenerPorSlugAsync(slug);

        if (manicurista is null)
            return NotFound();

        return Ok(new
        {
            manicurista.TenantId,
            manicurista.Nombre,
            manicurista.Slug,
            manicurista.WhatsApp,
            manicurista.DuracionTurnoMinutos
        });
    }

    [HttpGet("servicios/{slug}")]
    public async Task<IActionResult> ObtenerServicios(string slug)
    {
        var manicurista = await _manicuristas.ObtenerPorSlugAsync(slug);

        if (manicurista is null)
            return NotFound();

        var servicios = await _servicios.ObtenerActivosPorTenantAsync(manicurista.TenantId);

        return Ok(servicios.Select(s => new { s.Id, s.Nombre, s.Precio }));
    }
}