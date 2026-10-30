namespace AgendaSaaS.Services;

public class AgendaClock : IClock
{
    public const string ZonaPorDefecto = "America/Argentina/Buenos_Aires";

    private readonly TimeZoneInfo _zona;

    public AgendaClock(
        IConfiguration configuration)
    {
        var id = configuration["App:TimeZone"];

        _zona = BuscarZona(string.IsNullOrWhiteSpace(id) ? ZonaPorDefecto : id);
    }

    // Linux y Windows modernos aceptan el id IANA; algunos Windows solo reconocen
    // el nombre propio ("Argentina Standard Time"), así que se prueba la conversión.
    private static TimeZoneInfo BuscarZona(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException) when (
            TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
    }

    public DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zona);
}
