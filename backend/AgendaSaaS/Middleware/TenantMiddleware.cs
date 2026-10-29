using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;

namespace AgendaSaaS.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantProvider tenantProvider,
        IManicuristaRepository manicuristaRepository)
    {
        var path = context.Request.Path;

        // Endpoints no scopeados a un tenant.
        // /api/superadmin opera sobre todas las manicuristas y se protege por rol.
        // /api/public resuelve el tenant por slug.
        // Lo que no es /api (ej. /health) no tiene tenant.
        if (!path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/api/auth") ||
            path.StartsWithSegments("/api/public") ||
            path.StartsWithSegments("/api/superadmin"))
        {
            await _next(context);
            return;
        }

        // Requests autenticados (panel admin): el tenant SIEMPRE se deriva del
        // token, nunca de un header que el cliente pueda manipular. Esto evita
        // que una manicurista acceda a los datos de otra cambiando X-Tenant-Id.
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst("tenantId")?.Value;

            if (!Guid.TryParse(claim, out var tenantIdClaim))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "El token no tiene un tenant asociado."
                });
                return;
            }

            // El JWT sigue siendo válido hasta que expira: si el superadmin desactivó
            // (o borró) a la manicurista, se corta el acceso en el próximo request.
            if (context.User.IsInRole(nameof(UsuarioRol.Manicurista)))
            {
                var manicurista =
                    await manicuristaRepository.ObtenerPorTenantAsync(tenantIdClaim);

                if (manicurista is null || !manicurista.Activo)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "La cuenta está desactivada."
                    });
                    return;
                }
            }

            tenantProvider.TenantId = tenantIdClaim;
            await _next(context);
            return;
        }

        // Request anónimo (o con token vencido) a un endpoint protegido: se deja
        // seguir para que la autorización responda 401 y el frontend mande al login.
        if (!path.StartsWithSegments("/api/turnos"))
        {
            await _next(context);
            return;
        }

        // Requests anónimos a endpoints públicos scopeados (reserva de clientas):
        // no hay identidad, así que el tenant viene en el header. Estos endpoints
        // solo exponen datos públicos o crean turnos pendientes.
        if (!context.Request.Headers.TryGetValue(
                "X-Tenant-Id",
                out var tenantHeader))
        {
            context.Response.StatusCode = 400;

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Falta el identificador de la manicurista"
            });

            return;
        }

        if (!Guid.TryParse(
                tenantHeader,
                out var tenantId))
        {
            context.Response.StatusCode = 400;

            await context.Response.WriteAsJsonAsync(new
            {
                error = "TenantId inválido"
            });

            return;
        }

        tenantProvider.TenantId = tenantId;

        await _next(context);
    }
}
