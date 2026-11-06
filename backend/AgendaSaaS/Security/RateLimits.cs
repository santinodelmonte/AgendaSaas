using System.Threading.RateLimiting;

namespace AgendaSaaS.Security;

/// <summary>
/// Límites por IP para los endpoints anónimos que se pueden abusar:
/// login (fuerza bruta de contraseñas) y reserva pública (llenar la agenda
/// de turnos pendientes falsos).
/// </summary>
public static class RateLimits
{
    public const string Login = "login";
    public const string Reservas = "reservas";

    public static IServiceCollection AddAgendaRateLimits(
        this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        message =
                            "Demasiados intentos. Esperá unos minutos y volvé a probar."
                    },
                    cancellationToken);
            };

            options.AddPolicy(Login, context =>
                PorIp(context, permitidos: 5, ventana: TimeSpan.FromMinutes(1)));

            options.AddPolicy(Reservas, context =>
                PorIp(context, permitidos: 5, ventana: TimeSpan.FromMinutes(15)));
        });
    }

    private static RateLimitPartition<string> PorIp(
        HttpContext context,
        int permitidos,
        TimeSpan ventana)
    {
        var ip =
            context.Connection.RemoteIpAddress?.ToString()
            ?? "desconocida";

        return RateLimitPartition.GetFixedWindowLimiter(
            ip,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitidos,
                Window = ventana,
                QueueLimit = 0
            });
    }
}
