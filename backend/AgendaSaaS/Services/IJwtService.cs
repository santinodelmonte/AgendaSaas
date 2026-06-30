using AgendaSaaS.Entities;

namespace AgendaSaaS.Services;

public interface IJwtService
{
    string GenerarToken(
        Usuario usuario);
}