using System.Net;
using System.Net.Http.Json;
using AgendaSaaS.Tests.Infra;

namespace AgendaSaaS.Tests;

public class ReservaPublicaTests : ApiTestBase
{
    public ReservaPublicaTests(ApiFactory factory) : base(factory) { }

    [MySqlFact]
    public async Task Perfil_y_servicios_publicos_por_slug()
    {
        var mani = await CrearManicuristaAsync();

        await LeerAsync(await mani.Admin.PostAsJsonAsync(
            "/api/admin/servicios", new { nombre = "Kapping", precio = 15000m }));
        var oculto = await LeerAsync(await mani.Admin.PostAsJsonAsync(
            "/api/admin/servicios", new { nombre = "Retirado", precio = 1m }));
        await LeerAsync(await mani.Admin.PutAsJsonAsync(
            $"/api/admin/servicios/{oculto.GetProperty("id").GetGuid()}",
            new { nombre = "Retirado", precio = 1m, activo = false }));

        var perfil = await LeerAsync(await Anonimo().GetAsync($"/api/public/perfil/{mani.Slug}"));
        Assert.Equal(mani.TenantId, perfil.GetProperty("tenantId").GetGuid());

        var servicios = await LeerAsync(await Anonimo().GetAsync($"/api/public/servicios/{mani.Slug}"));
        var nombres = servicios.EnumerateArray().Select(s => s.GetProperty("nombre").GetString()).ToList();
        Assert.Equal(["Kapping"], nombres);

        await AssertStatusAsync(
            HttpStatusCode.NotFound,
            await Anonimo().GetAsync("/api/public/perfil/no-existe-este-slug"));
    }

    [MySqlFact]
    public async Task Disponibles_devuelve_la_grilla_del_horario_por_defecto()
    {
        var mani = await CrearManicuristaAsync();

        var lunes = await DisponiblesAsync(mani.TenantId, ProximoLunes);

        // Lunes a viernes de 09:00 a 18:00, turnos de 30 minutos.
        Assert.Equal(18, lunes.Count);
        Assert.Equal(ProximoLunes.AddHours(9), lunes.First().GetProperty("fechaHora").GetDateTime());
        Assert.Equal(ProximoLunes.AddHours(17.5), lunes.Last().GetProperty("fechaHora").GetDateTime());
        Assert.All(lunes, s => Assert.Equal("Disponible", s.GetProperty("estado").GetString()));

        var domingo = await DisponiblesAsync(mani.TenantId, ProximoLunes.AddDays(-1));
        Assert.Empty(domingo);
    }

    [MySqlFact]
    public async Task Disponibles_no_ofrece_horarios_dentro_de_la_anticipacion_minima()
    {
        var mani = await CrearManicuristaAsync();

        // "Ahora" son las 08:00: con 2 h de anticipación, el primero reservable es 10:30.
        var hoy = await DisponiblesAsync(mani.TenantId, Clock.Now.Date);

        Assert.Equal(Clock.Now.Date.AddHours(10.5), hoy.First().GetProperty("fechaHora").GetDateTime());
    }

    [MySqlFact]
    public async Task Solicitar_turno_lo_deja_pendiente_y_ocupa_el_horario()
    {
        var mani = await CrearManicuristaAsync();
        var horario = ProximoLunes.AddHours(10);

        await LeerAsync(await SolicitarAsync(mani.TenantId, horario, "Ana"));

        var slot = (await DisponiblesAsync(mani.TenantId, ProximoLunes))
            .Single(s => s.GetProperty("fechaHora").GetDateTime() == horario);
        Assert.Equal("Pendiente", slot.GetProperty("estado").GetString());

        var pendientes = await LeerAsync(await mani.Admin.GetAsync("/api/admin/turnos/pendientes"));
        var pendiente = Assert.Single(pendientes.EnumerateArray());
        Assert.Equal("Ana", pendiente.GetProperty("nombreCliente").GetString());

        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, horario, "Beatriz"));
    }

    [MySqlFact]
    public async Task Solicitar_rechaza_horarios_invalidos()
    {
        var mani = await CrearManicuristaAsync();

        // En el pasado.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, Clock.Now.AddHours(-1)));
        // Dentro de las 2 h de anticipación.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, Clock.Now.Date.AddHours(9.5)));
        // Fuera de la grilla de 30 minutos.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, ProximoLunes.AddHours(10.25)));
        // Fuera del horario laboral.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, ProximoLunes.AddHours(19)));
        // Sábado: sin horario configurado.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, ProximoLunes.AddDays(5).AddHours(10)));
        // Nombre vacío.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, ProximoLunes.AddHours(10), "  "));
    }

    [MySqlFact]
    public async Task Reservas_simultaneas_del_mismo_horario_solo_acepta_una()
    {
        var mani = await CrearManicuristaAsync();
        var horario = ProximoLunes.AddHours(11);

        // Se precalienta el pool de conexiones: si no, abrir conexiones nuevas tarda
        // más que la reserva entera y las requests terminan serializadas.
        await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => Task.Run(() => DisponiblesAsync(mani.TenantId, ProximoLunes))));

        // Task.Run: cada reserva en su propio hilo, como en Kestrel (MySql.Data
        // resuelve parte de su "async" de forma sincrónica).
        var respuestas = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(i => Task.Run(() => SolicitarAsync(mani.TenantId, horario, $"Clienta {i}"))));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(7, respuestas.Count(r => r.StatusCode == HttpStatusCode.BadRequest));

        var dia = await LeerAsync(await mani.Admin.GetAsync($"/api/admin/agenda?fecha={Fecha(ProximoLunes)}"));
        Assert.Single(dia.EnumerateArray());
    }

    [MySqlFact]
    public async Task Dia_bloqueado_no_ofrece_ni_acepta_reservas()
    {
        var mani = await CrearManicuristaAsync();

        await LeerAsync(await mani.Admin.PostAsJsonAsync(
            "/api/admin/dias-bloqueados", new { fecha = Fecha(ProximoLunes), motivo = "Feriado" }));

        Assert.Empty(await DisponiblesAsync(mani.TenantId, ProximoLunes));
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, ProximoLunes.AddHours(10)));
    }

    [MySqlFact]
    public async Task Endpoints_publicos_de_turnos_requieren_tenant_valido()
    {
        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await Anonimo().GetAsync($"/api/turnos/disponibles?fecha={Fecha(ProximoLunes)}"));

        await AssertStatusAsync(
            HttpStatusCode.NotFound,
            await Publico(Guid.NewGuid()).GetAsync($"/api/turnos/disponibles?fecha={Fecha(ProximoLunes)}"));
    }
}
