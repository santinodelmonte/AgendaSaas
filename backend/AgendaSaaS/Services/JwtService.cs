using AgendaSaaS.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AgendaSaaS.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerarToken(
        Usuario usuario)
    {
        var key =
            _configuration["Jwt:Key"]!;

        var issuer =
            _configuration["Jwt:Issuer"]!;

        var audience =
            _configuration["Jwt:Audience"]!;

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    usuario.Id.ToString()),

                new Claim(
                    "tenantId",
                    usuario.TenantId.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    usuario.Email),

                new Claim(
                    ClaimTypes.Role,
                    usuario.Rol.ToString())
            };

        var token =
            new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires:
                    DateTime.UtcNow.AddDays(7),
                signingCredentials:
                    credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}