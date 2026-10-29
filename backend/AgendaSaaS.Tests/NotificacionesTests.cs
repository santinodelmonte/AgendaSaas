using System.Net;
using System.Net.Http.Json;
using AgendaSaaS.Services.Notificaciones;
using AgendaSaaS.Tests.Infra;

namespace AgendaSaaS.Tests;

public class NotificacionesTests : ApiTestBase
{
    public NotificacionesTests(ApiFactory factory) : base(factory) { }

    private static string NuevoEndpoint() =>
        $"https://push.tests.local/{Guid.NewGuid():N}";

    private static async Task SuscribirAsync(HttpClient admin, string endpoint)
    {
        await LeerAsync(await admin.PostAsJsonAsync("/api/admin/notificaciones/suscripciones", new
        {
            endpoint,
            keys = new { p256dh = "clave-p256dh", auth = "clave-auth" }
        }));
    }

    private async Task<List<NotificacionPush>> EsperarAsync(string endpoint, int cantidad = 1)
    {
        // El aviso de reserva se manda en segundo plano.
        for (var i = 0; i < 50 && Factory.Push.Para(endpoint).Count < cantidad; i++)
            await Task.Delay(100);

        return Factory.Push.Para(endpoint);
    }

    [MySqlFact]
    public async Task La_clave_publica_se_genera_una_vez_y_se_mantiene()
    {
        var mani = await CrearManicuristaAsync();

        var primera = await LeerAsync(await mani.Admin.GetAsync("/api/admin/notificaciones/clave-publica"));
        var segunda = await LeerAsync(await mani.Admin.GetAsync("/api/admin/notificaciones/clave-publica"));

        var clave = primera.GetProperty("publicKey").GetString();
        Assert.False(string.IsNullOrWhiteSpace(clave));
        Assert.Equal(clave, segunda.GetProperty("publicKey").GetString());
    }

    [MySqlFact]
    public async Task Una_reserva_avisa_solo_a_los_dispositivos_de_esa_manicurista()
    {
        var a = await CrearManicuristaAsync();
        var b = await CrearManicuristaAsync();
        var celularA = NuevoEndpoint();
        var tabletA = NuevoEndpoint();
        var celularB = NuevoEndpoint();
        await SuscribirAsync(a.Admin, celularA);
        await SuscribirAsync(a.Admin, tabletA);
        await SuscribirAsync(b.Admin, celularB);

        await LeerAsync(await SolicitarAsync(a.TenantId, ProximoLunes.AddHours(10), "Ana"));

        var aviso = Assert.Single(await EsperarAsync(celularA));
        Assert.Single(await EsperarAsync(tabletA));
        Assert.Equal("Ana · lun 14/1 10:00 · Semipermanente", aviso.Cuerpo);
        Assert.Equal("/admin/turnos-pendientes", aviso.Url);
        Assert.Empty(Factory.Push.Para(celularB));
    }

    [MySqlFact]
    public async Task Reserva_sin_dispositivos_suscriptos_funciona_igual()
    {
        var mani = await CrearManicuristaAsync();

        await LeerAsync(await SolicitarAsync(mani.TenantId, ProximoLunes.AddHours(10)));
    }

    [MySqlFact]
    public async Task Prueba_cuenta_envios_y_borra_suscripciones_vencidas()
    {
        var mani = await CrearManicuristaAsync();
        var activo = NuevoEndpoint();
        var vencido = NuevoEndpoint();
        Factory.Push.Vencidos[vencido] = true;
        await SuscribirAsync(mani.Admin, activo);
        await SuscribirAsync(mani.Admin, vencido);

        var primera = await LeerAsync(await mani.Admin.PostAsync("/api/admin/notificaciones/prueba", null));
        Assert.Equal(1, primera.GetProperty("enviados").GetInt32());

        await LeerAsync(await mani.Admin.PostAsync("/api/admin/notificaciones/prueba", null));

        // El vencido se intentó una sola vez: después de la primera prueba se borró.
        Assert.Single(Factory.Push.Para(vencido));
        Assert.Equal(2, Factory.Push.Para(activo).Count);
    }

    [MySqlFact]
    public async Task Desuscribir_deja_de_enviar_y_no_afecta_a_otras_cuentas()
    {
        var a = await CrearManicuristaAsync();
        var b = await CrearManicuristaAsync();
        var endpoint = NuevoEndpoint();
        await SuscribirAsync(a.Admin, endpoint);

        // B no puede borrar la suscripción de A aunque conozca el endpoint.
        await LeerAsync(await b.Admin.PostAsJsonAsync("/api/admin/notificaciones/suscripciones/eliminar", new { endpoint }));
        var antes = await LeerAsync(await a.Admin.PostAsync("/api/admin/notificaciones/prueba", null));
        Assert.Equal(1, antes.GetProperty("enviados").GetInt32());

        await LeerAsync(await a.Admin.PostAsJsonAsync("/api/admin/notificaciones/suscripciones/eliminar", new { endpoint }));
        var despues = await LeerAsync(await a.Admin.PostAsync("/api/admin/notificaciones/prueba", null));
        Assert.Equal(0, despues.GetProperty("enviados").GetInt32());
    }

    [MySqlFact]
    public async Task Volver_a_suscribir_el_mismo_dispositivo_no_lo_duplica()
    {
        var mani = await CrearManicuristaAsync();
        var endpoint = NuevoEndpoint();
        await SuscribirAsync(mani.Admin, endpoint);
        await SuscribirAsync(mani.Admin, endpoint);

        var prueba = await LeerAsync(await mani.Admin.PostAsync("/api/admin/notificaciones/prueba", null));

        Assert.Equal(1, prueba.GetProperty("enviados").GetInt32());
    }

    [MySqlFact]
    public async Task Suscripcion_invalida_se_rechaza()
    {
        var mani = await CrearManicuristaAsync();

        await AssertStatusAsync(HttpStatusCode.BadRequest, await mani.Admin.PostAsJsonAsync(
            "/api/admin/notificaciones/suscripciones",
            new { endpoint = "http://inseguro.local/x", keys = new { p256dh = "a", auth = "b" } }));

        await AssertStatusAsync(HttpStatusCode.BadRequest, await mani.Admin.PostAsJsonAsync(
            "/api/admin/notificaciones/suscripciones",
            new { endpoint = NuevoEndpoint() }));

        await AssertStatusAsync(HttpStatusCode.Unauthorized, await Anonimo().PostAsJsonAsync(
            "/api/admin/notificaciones/suscripciones",
            new { endpoint = NuevoEndpoint(), keys = new { p256dh = "a", auth = "b" } }));
    }
}
