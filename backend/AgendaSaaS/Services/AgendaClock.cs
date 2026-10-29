namespace AgendaSaaS.Services;

public class AgendaClock : IClock
{
    public const string ZonaPorDefecto = "America/Argentina/Buenos_Aires";

    private readonly TimeZoneInfo _zona;

    public AgendaClock(
        IConfiguration configuration)
    {
        var id = configuration["App:TimeZone"];

        _zona = TimeZoneInfo.FindSystemTimeZoneById(
            string.IsNullOrWhiteSpace(id) ? ZonaPorDefecto : id);
    }

    public DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zona);
}
