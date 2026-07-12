using AgendaSaaS.Data;
using AgendaSaaS.Middleware;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
// CORS
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("AgendaSaaS", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:5175",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:5174",
                "http://127.0.0.1:5175"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
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
}

app.UseSwagger();
app.UseSwaggerUI();

//
// CORS debe ejecutarse antes de Authentication y de HttpsRedirection
//
app.UseCors("AgendaSaaS");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();