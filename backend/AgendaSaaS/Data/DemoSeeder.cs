using AgendaSaaS.Entities;
using AgendaSaaS.Repositories;
using AgendaSaaS.Services;

namespace AgendaSaaS.Data;

/// <summary>
/// Datos de prueba para demos locales (Demo:Seed = true, activado en el perfil de
/// desarrollo). Crea una manicurista con horarios, servicios y turnos alrededor de
/// la fecha actual. No hace nada si ya existe.
/// </summary>
public static class DemoSeeder
{
    public const string Slug = "unas-de-caro";
    public const string Email = "caro@demo.local";
    public const string Password = "demo1234";

    private static readonly string[] Clientas =
    [
        "Ana López", "Belén Gómez", "Camila Díaz", "Daniela Ruiz", "Florencia Paz",
        "Julieta Sosa", "Lucía Romero", "Micaela Torres", "Paula Benítez", "Sofía Herrera",
        "Valentina Castro", "Agustina Molina"
    ];

    private static readonly (string Nombre, decimal Precio)[] Servicios =
    [
        ("Semipermanente", 12000m),
        ("Kapping gel", 15000m),
        ("Esculpidas", 22000m),
        ("Belleza de pies", 14000m),
        ("Retiro + manicura", 8000m)
    ];

    public static async Task SembrarAsync(IServiceProvider services, ILogger logger)
    {
        var manicuristas = services.GetRequiredService<IManicuristaRepository>();

        if (await manicuristas.ObtenerPorSlugAsync(Slug) != null)
            return;

        var clock = services.GetRequiredService<IClock>();
        var tenantId = Guid.NewGuid();

        var horarios = new List<BloqueHorarioConfig>();

        // Lunes a viernes 9 a 19 con pausa de 13 a 14; sábados 9 a 13.
        for (var dia = DayOfWeek.Monday; dia <= DayOfWeek.Friday; dia++)
            horarios.Add(Bloque(tenantId, dia, 9, 19, 13, 14));
        horarios.Add(Bloque(tenantId, DayOfWeek.Saturday, 9, 13, null, null));

        await services.GetRequiredService<ISuperAdminRepository>().CrearAsync(
            new Manicurista
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Nombre = "Uñas de Caro",
                Slug = Slug,
                Email = Email,
                WhatsApp = "+5491155550000",
                Activo = true,
                DuracionTurnoMinutos = 60
            },
            new Usuario
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                Activo = true,
                Rol = UsuarioRol.Manicurista
            },
            horarios);

        var servicios = services.GetRequiredService<IServicioRepository>();
        foreach (var (nombre, precio) in Servicios)
            await servicios.CrearAsync(new Servicio { TenantId = tenantId, Nombre = nombre, Precio = precio, Activo = true });

        var turnos = services.GetRequiredService<ITurnoRepository>();
        var ahora = clock.Now;
        var random = new Random(42);
        var creados = 0;

        // Desde 3 meses atrás hasta 2 semanas adelante: historial y métricas con
        // datos, agenda de esta semana llena y algunos pendientes por confirmar.
        for (var fecha = ahora.Date.AddMonths(-3); fecha <= ahora.Date.AddDays(14); fecha = fecha.AddDays(1))
        {
            var bloque = horarios.FirstOrDefault(h => h.DiaSemana == fecha.DayOfWeek);
            if (bloque is null)
                continue;

            var esPasado = fecha < ahora.Date;
            var ocupacion = esPasado ? 0.45 : fecha <= ahora.Date.AddDays(7) ? 0.6 : 0.3;

            for (var hora = bloque.HoraInicio; hora < bloque.HoraFin; hora += TimeSpan.FromHours(1))
            {
                if (bloque.PausaInicio <= hora && hora < bloque.PausaFin)
                    continue;

                if (random.NextDouble() > ocupacion)
                    continue;

                var fechaHora = fecha + hora;
                var futuro = fechaHora > ahora;

                var estado =
                    !futuro ? (random.NextDouble() < 0.08 ? TurnoEstado.Rechazado : TurnoEstado.Confirmado)
                    : random.NextDouble() < 0.25 ? TurnoEstado.Pendiente
                    : TurnoEstado.Confirmado;

                await turnos.CrearAsync(new Turno
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FechaHora = fechaHora,
                    NombreCliente = Clientas[random.Next(Clientas.Length)],
                    TelefonoCliente = $"11 {random.Next(2000, 6999)}-{random.Next(1000, 9999)}",
                    ServicioSolicitado = Servicios[random.Next(Servicios.Length)].Nombre,
                    Estado = estado,
                    FechaCreacionPendiente = estado == TurnoEstado.Pendiente ? DateTime.UtcNow : null,
                    DuracionMinutos = random.NextDouble() < 0.15 ? 90 : null,
                    Version = 1
                });

                creados++;
            }
        }

        logger.LogInformation(
            "Datos de demo creados: {Email} / {Password} ({Turnos} turnos). Agenda pública: /agenda/{Slug}",
            Email, Password, creados, Slug);
    }

    private static BloqueHorarioConfig Bloque(
        Guid tenantId, DayOfWeek dia, int inicio, int fin, int? pausaInicio, int? pausaFin) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        DiaSemana = dia,
        HoraInicio = TimeSpan.FromHours(inicio),
        HoraFin = TimeSpan.FromHours(fin),
        PausaInicio = pausaInicio is null ? null : TimeSpan.FromHours(pausaInicio.Value),
        PausaFin = pausaFin is null ? null : TimeSpan.FromHours(pausaFin.Value)
    };
}
