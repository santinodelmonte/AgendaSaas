using AgendaSaaS.DTOs;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/perfil")]
public class AdminPerfilController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IManicuristaRepository _repository;

    public AdminPerfilController(
        ITenantProvider tenantProvider,
        IManicuristaRepository repository)
    {
        _tenantProvider = tenantProvider;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var manicurista =
            await _repository.ObtenerPorTenantAsync(
                _tenantProvider.TenantId);

        if (manicurista is null)
            return NotFound("Manicurista no encontrada.");

        return Ok(manicurista);
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar(
        ActualizarPerfilManicuristaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest("Nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.WhatsApp))
            return BadRequest("WhatsApp es obligatorio.");

        if (request.DuracionTurnoMinutos <= 0)
            return BadRequest(
                "DuracionTurnoMinutos debe ser mayor que cero.");

        var manicurista =
            await _repository.ObtenerPorTenantAsync(
                _tenantProvider.TenantId);

        if (manicurista is null)
            return NotFound("Manicurista no encontrada.");

        manicurista.Nombre = request.Nombre.Trim();
        manicurista.Email = request.Email.Trim();
        manicurista.WhatsApp = request.WhatsApp.Trim();
        manicurista.DuracionTurnoMinutos =
            request.DuracionTurnoMinutos;

        await _repository.ActualizarAsync(manicurista);

        return Ok(manicurista);
    }
}