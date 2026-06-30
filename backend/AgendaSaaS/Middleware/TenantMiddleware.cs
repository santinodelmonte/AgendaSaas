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

        // Endpoints públicos
        if (path.StartsWithSegments("/api/auth") ||
            path.StartsWithSegments("/api/public"))
        {
            await _next(context);
            return;
        }

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