using AgendaSaaS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IManicuristaRepository _repository;

    public PublicController(
        IManicuristaRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("perfil/{slug}")]
    public async Task<IActionResult> ObtenerPerfil(
        string slug)
    {
        var manicurista =
            await _repository
                .ObtenerPorSlugAsync(slug);

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
}