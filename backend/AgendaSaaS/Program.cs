using AgendaSaaS.Data;
using AgendaSaaS.Middleware;
using AgendaSaaS.Repositories;
using AgendaSaaS.Security;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//
// CORS: en prod los orígenes vienen de config (Cors:Origins, separados por coma,
// o la variable de entorno Cors__Origins). En desarrollo, si no hay, se usa Vite local.
//
var corsOrigins =
    (builder.Configuration["Cors:Origins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

if (corsOrigins.Length == 0)
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException(
            "Cors:Origins no configurada (ej: Cors__Origins=https://app.midominio.com).");

    corsOrigins =
    [
        "http://localhost:5173",
        "http://localhost:5174",
        "http://localhost:5175",
        "http://127.0.0.1:5173",
        "http://127.0.0.1:5174",
        "http://127.0.0.1:5175"
    ];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AgendaSaaS", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAgendaRateLimits();

// Detrás de un proxy reverso (Nginx, Caddy) la IP real llega en X-Forwarded-For;
// sin esto el rate limiting por IP vería a todos los clientes como el proxy.
// Por defecto solo se confía en proxies en loopback.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddSingleton<SqlConnectionFactory>();

builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.AddScoped<IManicuristaRepository, ManicuristaRepository>();
builder.Services.AddScoped<IBloqueRepository, BloqueRepository>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IDiasBloqueadosRepository, DiasBloqueadosRepository>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<ITurnoLock, TurnoLock>();

builder.Services.AddHostedService<TurnoExpirationService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key =
            builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key no configurada.");

        var issuer =
            builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Jwt:Issuer no configurada.");

        var audience =
            builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Jwt:Audience no configurada.");

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(key)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

        // Un token firmado sigue siendo válido hasta que vence. Para que
        // desactivar a una usuaria o resetearle la contraseña (superadmin) le corte
        // el acceso en el acto, se verifica en cada request que siga activa y que
        // el token sea de la versión vigente.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sub =
                    context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var usuarios =
                    context.HttpContext.RequestServices
                        .GetRequiredService<IUsuarioRepository>();

                var versionToken =
                    context.Principal?.FindFirst("tokenVersion")?.Value;

                var versionVigente =
                    Guid.TryParse(sub, out var usuarioId)
                        ? await usuarios.ObtenerTokenVersionSiActivoAsync(usuarioId)
                        : null;

                if (versionVigente is null ||
                    versionToken != versionVigente.Value.ToString())
                {
                    context.Fail("Sesión inválida: usuario desactivado o contraseña cambiada.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

var app = builder.Build();

// Errores no controlados: se loguean (lo hace el middleware) y la respuesta tiene
// el mismo formato { message } que lee el front, sin exponer detalles internos.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error =
        context.Features.Get<IExceptionHandlerFeature>()?.Error;

    var (status, message) =
        error is LockNoDisponibleException
            ? (StatusCodes.Status409Conflict, error.Message)
            : (StatusCodes.Status500InternalServerError,
               "Ocurrió un error inesperado. Probá de nuevo en unos minutos.");

    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new { message });
}));

await SchemaMigrations.AplicarAsync(
    app.Services.GetRequiredService<SqlConnectionFactory>());

using (var scope = app.Services.CreateScope())
{
    await SuperAdminSeeder.SembrarAsync(
        scope.ServiceProvider.GetRequiredService<IUsuarioRepository>(),
        app.Configuration,
        app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("SuperAdminSeeder"));
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//
// CORS debe ejecutarse antes de Authentication y de HttpsRedirection
//
app.UseCors("AgendaSaaS");

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();

app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Para el hosting / monitoreo: 200 si la API responde y llega a la base.
app.MapGet("/health", async (SqlConnectionFactory factory) =>
{
    try
    {
        using var connection = factory.CreateConnection();
        await connection.OpenAsync();
        return Results.Ok(new { status = "ok" });
    }
    catch
    {
        return Results.Json(
            new { status = "db-unreachable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.Run();