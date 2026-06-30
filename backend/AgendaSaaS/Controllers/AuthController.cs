using AgendaSaaS.DTOs;
using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IJwtService _jwtService;

    public AuthController(
        IUsuarioRepository usuarioRepository,
        IJwtService jwtService)
    {
        _usuarioRepository =
            usuarioRepository;

        _jwtService =
            jwtService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var usuario =
            await _usuarioRepository
                .ObtenerPorEmailAsync(
                    request.Email);

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (!usuario.Activo)
        {
            return Unauthorized();
        }

        var passwordValida =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                usuario.PasswordHash);

        if (!passwordValida)
        {
            return Unauthorized();
        }

        var token =
            _jwtService
                .GenerarToken(usuario);

        return Ok(new
        {
            token,
            usuario.Email,
            usuario.TenantId
        });
    }

    [HttpPost("crear-admin")]
    public async Task<IActionResult> CrearAdmin(
    CrearAdminRequest request)
    {
        var existente =
            await _usuarioRepository
                .ObtenerPorEmailAsync(
                    request.Email);

        if (existente is not null)
        {
            return BadRequest(
                "Ya existe un usuario con ese email.");
        }

        var usuario =
            new Usuario
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Email = request.Email,
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        request.Password),
                Activo = true
            };

        await _usuarioRepository
            .CrearAsync(usuario);

        return Ok(new
        {
            usuario.Id,
            usuario.Email,
            usuario.TenantId
        });
    }
}