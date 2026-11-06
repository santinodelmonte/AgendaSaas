namespace AgendaSaaS.Services;

/// <summary>
/// Los turnos se guardan como hora de pared argentina (sin zona). El servidor
/// puede correr en UTC, así que "ahora" nunca se toma de DateTime.Now: se
/// convierte explícitamente a la zona del negocio.
/// </summary>
public static class HoraLocal
{
    private static readonly TimeZoneInfo Zona =
        TimeZoneInfo.FindSystemTimeZoneById(
            "America/Argentina/Buenos_Aires");

    public static DateTime Ahora =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    public static DateTime Hoy => Ahora.Date;
}
