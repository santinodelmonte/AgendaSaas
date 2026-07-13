using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AgendaSaaS.DTOs;
using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/superadmin/manicuristas")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminRepository _superAdminRepository;
    private readonly IManicuristaRepository _manicuristaRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public SuperAdminController(
        ISuperAdminRepository superAdminRepository,
        IManicuristaRepository manicuristaRepository,
        IUsuarioRepository usuarioRepository)
    {
        _superAdminRepository = superAdminRepository;
        _manicuristaRepository = manicuristaRepository;
        _usuarioRepository = usuarioRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _superAdminRepository.ListarAsync();
        return Ok(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearManicuristaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("El email es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.WhatsApp))
            return BadRequest("El WhatsApp es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest("La contraseña debe tener al menos 6 caracteres.");

        var email = request.Email.Trim().ToLowerInvariant();

        var usuarioExistente = await _usuarioRepository.ObtenerPorEmailAsync(email);
        if (usuarioExistente != null)
            return BadRequest("Ya existe un usuario con ese email.");

        var duracion = request.DuracionTurnoMinutos > 0 ? request.DuracionTurnoMinutos : 30;

        var slugBase = string.IsNullOrWhiteSpace(request.Slug)
            ? Slugify(request.Nombre)
            : Slugify(request.Slug);

        if (string.IsNullOrWhiteSpace(slugBase))
            return BadRequest("No se pudo generar un slug válido a partir del nombre.");

        var slug = await GenerarSlugUnicoAsync(slugBase);

        var tenantId = Guid.NewGuid();

        var manicurista = new Manicurista
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Nombre = request.Nombre.Trim(),
            Slug = slug,
            Email = email,
            WhatsApp = request.WhatsApp.Trim(),
            Activo = true,
            DuracionTurnoMinutos = duracion
        };

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Activo = true,
            Rol = UsuarioRol.Manicurista
        };

        var horarios = HorariosPorDefecto(tenantId);

        await _superAdminRepository.CrearAsync(manicurista, usuario, horarios);

        return Ok(new
        {
            manicurista.TenantId,
            manicurista.Nombre,
            manicurista.Slug,
            manicurista.Email,
            manicurista.WhatsApp,
            manicurista.DuracionTurnoMinutos,
            manicurista.Activo
        });
    }

    [HttpPut("{tenantId}")]
    public async Task<IActionResult> Actualizar(Guid tenantId, ActualizarManicuristaRequest request)
    {
        var manicurista = await _manicuristaRepository.ObtenerPorTenantAsync(tenantId);
        if (manicurista is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.WhatsApp))
            return BadRequest("El WhatsApp es obligatorio.");

        var duracion = request.DuracionTurnoMinutos > 0 ? request.DuracionTurnoMinutos : 30;

        var slugBase = string.IsNullOrWhiteSpace(request.Slug)
            ? Slugify(request.Nombre)
            : Slugify(request.Slug);

        if (string.IsNullOrWhiteSpace(slugBase))
            return BadRequest("No se pudo generar un slug válido.");

        // Si el slug no cambió, se conserva; si cambió, se valida unicidad.
        var slug = slugBase == manicurista.Slug
            ? manicurista.Slug
            : await GenerarSlugUnicoAsync(slugBase);

        await _superAdminRepository.ActualizarAsync(
            tenantId, request.Nombre.Trim(), slug, request.WhatsApp.Trim(), duracion);

        return Ok(new { tenantId, nombre = request.Nombre.Trim(), slug, duracionTurnoMinutos = duracion });
    }

    [HttpPut("{tenantId}/activo")]
    public async Task<IActionResult> SetActivo(Guid tenantId, SetActivoRequest request)
    {
        var manicurista = await _manicuristaRepository.ObtenerPorTenantAsync(tenantId);
        if (manicurista is null)
            return NotFound();

        await _superAdminRepository.SetActivoAsync(tenantId, request.Activo);
        return Ok(new { tenantId, activo = request.Activo });
    }

    [HttpPost("{tenantId}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid tenantId, ResetPasswordRequest request)
    {
        var manicurista = await _manicuristaRepository.ObtenerPorTenantAsync(tenantId);
        if (manicurista is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest("La contraseña debe tener al menos 6 caracteres.");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await _superAdminRepository.ResetPasswordAsync(tenantId, hash);

        return Ok();
    }

    [HttpDelete("{tenantId}")]
    public async Task<IActionResult> Eliminar(Guid tenantId)
    {
        var manicurista = await _manicuristaRepository.ObtenerPorTenantAsync(tenantId);
        if (manicurista is null)
            return NotFound();

        await _superAdminRepository.EliminarAsync(tenantId);
        return Ok();
    }

    private async Task<string> GenerarSlugUnicoAsync(string slugBase)
    {
        var slug = slugBase;
        var sufijo = 2;

        while (await _manicuristaRepository.ObtenerPorSlugAsync(slug) != null)
        {
            slug = $"{slugBase}-{sufijo}";
            sufijo++;
        }

        return slug;
    }

    private static List<BloqueHorarioConfig> HorariosPorDefecto(Guid tenantId)
    {
        var horarios = new List<BloqueHorarioConfig>();

        // Lunes a viernes 09:00 - 18:00, sin pausa.
        for (var dia = DayOfWeek.Monday; dia <= DayOfWeek.Friday; dia++)
        {
            horarios.Add(new BloqueHorarioConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                DiaSemana = dia,
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(18, 0, 0),
                PausaInicio = null,
                PausaFin = null
            });
        }

        return horarios;
    }

    private static string Slugify(string input)
    {
        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c is ' ' or '-' or '_')
                sb.Append('-');
        }

        return Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
    }
}
