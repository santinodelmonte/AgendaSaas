using AgendaSaaS.Services;
using Microsoft.Extensions.Configuration;

namespace AgendaSaaS.Tests;

public class AgendaClockTests
{
    private static AgendaClock Crear(string? zona) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:TimeZone"] = zona })
            .Build());

    [Fact]
    public void Usa_la_hora_de_Argentina_por_defecto_aunque_el_servidor_este_en_UTC()
    {
        var diferencia = DateTime.UtcNow - Crear(null).Now;

        // Argentina es UTC-3 todo el año.
        Assert.InRange(diferencia.TotalMinutes, 179, 181);
    }

    [Fact]
    public void Respeta_la_zona_configurada()
    {
        var diferencia = Crear("Europe/Madrid").Now - DateTime.UtcNow;

        Assert.InRange(diferencia.TotalHours, 0.9, 2.1);
    }
}
