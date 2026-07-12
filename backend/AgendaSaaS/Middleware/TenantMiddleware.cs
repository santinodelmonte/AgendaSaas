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
        ITenantProvider tenantProvider)
    {
        var path = context.Request.Path;

        // Endpoints no scopeados a un tenant.
        // /api/superadmin opera sobre todas las manicuristas y se protege por rol.
        // /api/public resuelve el tenant por slug.
        if (path.StartsWithSegments("/api/auth") ||
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

            tenantProvider.TenantId = tenantIdClaim;
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
