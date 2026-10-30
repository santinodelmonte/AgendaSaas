using AgendaSaaS.Data;
using AgendaSaaS.Middleware;
using AgendaSaaS.Repositories;
using AgendaSaaS.Security;
using AgendaSaaS.Services;
using AgendaSaaS.Services.Notificaciones;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
// CORS: orígenes permitidos separados por coma (Cors:AllowedOrigins / Cors__AllowedOrigins).
// En producción tiene que ser la URL pública del frontend.
//
var corsOrigins =
    (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

//
// Detrás de un reverse proxy (nginx, Render, Railway, etc.) la IP real del cliente
// y el esquema llegan en X-Forwarded-*. Hace falta para que el rate limiting sea
// por IP de clienta y no por la IP del proxy.
//
var detrasDeProxy =
    builder.Configuration.GetValue("ReverseProxy:Enabled", false);

if (detrasDeProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddAgendaRateLimiting(builder.Configuration);

builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddSingleton<IClock, AgendaClock>();
builder.Services.AddSingleton<TenantLocks>();

builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.AddScoped<IManicuristaRepository, ManicuristaRepository>();
builder.Services.AddScoped<IBloqueRepository, BloqueRepository>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IDiasBloqueadosRepository, DiasBloqueadosRepository>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();
builder.Services.AddScoped<IPushSuscripcionRepository, PushSuscripcionRepository>();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddHostedService<TurnoExpirationService>();

// Notificaciones push a la manicurista (nueva reserva).
builder.Services.AddHttpClient(nameof(WebPushSender), client =>
    client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<VapidKeys>();
builder.Services.AddSingleton<Notificador>();
builder.Services.AddScoped<IPushSender, WebPushSender>();
builder.Services.AddHostedService<NotificacionesWorker>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key =
            builder.Configuration["Jwt:Key"];

        // HMAC-SHA256 necesita una clave de al menos 256 bits: con una más corta el
        // login falla recién al generar el token.
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException(
                "Jwt:Key no configurada o demasiado corta (mínimo 32 caracteres).");

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
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});

var app = builder.Build();

await SchemaMigrations.AplicarAsync(
    app.Services.GetRequiredService<SqlConnectionFactory>());

using (var scope = app.Services.CreateScope())
{
    await SuperAdminSeeder.SembrarAsync(
        scope.ServiceProvider.GetRequiredService<IUsuarioRepository>(),
        app.Configuration,
        app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("SuperAdminSeeder"));

    if (app.Configuration.GetValue("Demo:Seed", false))
    {
        await DemoSeeder.SembrarAsync(
            scope.ServiceProvider,
            app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoSeeder"));
    }
}

if (detrasDeProxy)
    app.UseForwardedHeaders();

// Cualquier excepción no controlada se loguea y devuelve un JSON genérico, sin
// exponer detalles internos.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Ocurrió un error inesperado. Probá de nuevo en unos minutos."
        });
    });
});

if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//
// CORS debe ejecutarse antes de Authentication y de HttpsRedirection
//
app.UseCors("AgendaSaaS");

if (!detrasDeProxy)
    app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();

app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Para el health check del hosting: responde 200 solo si la base está accesible.
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
            new { status = "db-unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.Run();

// Expuesto para los tests de integración (WebApplicationFactory<Program>).
public partial class Program;