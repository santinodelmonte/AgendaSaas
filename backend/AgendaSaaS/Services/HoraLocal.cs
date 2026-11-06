namespace AgendaSaaS.Services;

/// <summary>
/// Los turnos se guardan como hora de pared de Uruguay (sin zona). El servidor
/// puede correr en UTC, así que "ahora" nunca se toma de DateTime.Now: se
/// convierte explícitamente a la zona del negocio.
/// </summary>
public static class HoraLocal
{
    // IANA en Linux/Windows con ICU; ID de Windows como respaldo (hosting IIS).
    private static readonly TimeZoneInfo Zona = BuscarZona();

    private static TimeZoneInfo BuscarZona()
    {
        foreach (var id in new[] { "America/Montevideo", "Montevideo Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }

        throw new InvalidOperationException("No se encontró la zona horaria de Montevideo.");
    }

    public static DateTime Ahora =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    public static DateTime Hoy => Ahora.Date;
}
