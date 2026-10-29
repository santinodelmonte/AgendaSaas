using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendaSaaS.Security;

/// <summary>
/// Límites por IP para los endpoints anónimos que se pueden abusar: el login
/// (fuerza bruta de contraseñas) y la reserva pública (spam de turnos pendientes).
/// Los valores se pueden ajustar por configuración (RateLimiting:*).
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Reservas = "reservas";

    public static IServiceCollection AddAgendaRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var loginPorMinuto =
            configuration.GetValue("RateLimiting:LoginPorMinuto", 10);

        var reservasPorHora =
            configuration.GetValue("RateLimiting:ReservasPorHora", 20);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Demasiados intentos. Probá de nuevo en unos minutos." },
                    token);
            };

            options.AddPolicy(Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClaveCliente(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginPorMinuto,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy(Reservas, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ClaveCliente(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = reservasPorHora,
                        Window = TimeSpan.FromHours(1)
                    }));
        });

        return services;
    }

    private static string ClaveCliente(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido";
}
