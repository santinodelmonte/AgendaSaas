using System.Net;
using System.Net.Http.Json;
using AgendaSaaS.Tests.Infra;

namespace AgendaSaaS.Tests;

public class GestionTurnosTests : ApiTestBase
{
    public GestionTurnosTests(ApiFactory factory) : base(factory) { }

    private static object TurnoManual(DateTime fechaHora, int? duracion = null) => new
    {
        fechaHora = FechaHora(fechaHora),
        nombreCliente = "Clienta Manual",
        telefonoCliente = "1100000000",
        servicio = "Esculpidas",
        nota = "",
        duracionMinutos = duracion
    };

    [MySqlFact]
    public async Task Confirmar_un_pendiente_actualiza_el_dashboard()
    {
        var mani = await CrearManicuristaAsync();
        var horario = Clock.Now.Date.AddHours(15);

        await LeerAsync(await SolicitarAsync(mani.TenantId, horario));
        var id = await TurnoIdAsync(mani.Admin, horario);

        var antes = await LeerAsync(await mani.Admin.GetAsync("/api/admin/dashboard"));
        Assert.Equal(1, antes.GetProperty("turnosPendientes").GetInt32());

        await LeerAsync(await mani.Admin.PostAsync($"/api/admin/turnos/confirmar/{id}", null));

        var despues = await LeerAsync(await mani.Admin.GetAsync("/api/admin/dashboard"));
        Assert.Equal(0, despues.GetProperty("turnosPendientes").GetInt32());
        Assert.Equal(1, despues.GetProperty("confirmadosHoy").GetInt32());
        Assert.Equal(1, despues.GetProperty("confirmadosMes").GetInt32());
        Assert.Equal(id, despues.GetProperty("proximoTurno").GetProperty("id").GetGuid());

        // Confirmar dos veces no es válido.
        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await mani.Admin.PostAsync($"/api/admin/turnos/confirmar/{id}", null));
    }

    [MySqlFact]
    public async Task Rechazar_libera_el_horario_para_otra_reserva()
    {
        var mani = await CrearManicuristaAsync();
        var horario = ProximoLunes.AddHours(12);

        await LeerAsync(await SolicitarAsync(mani.TenantId, horario, "Primera"));
        var id = await TurnoIdAsync(mani.Admin, horario);

        await LeerAsync(await mani.Admin.PostAsync($"/api/admin/turnos/rechazar/{id}", null));

        var slot = (await DisponiblesAsync(mani.TenantId, ProximoLunes))
            .Single(s => s.GetProperty("fechaHora").GetDateTime() == horario);
        Assert.Equal("Disponible", slot.GetProperty("estado").GetString());

        await LeerAsync(await SolicitarAsync(mani.TenantId, horario, "Segunda"));
    }

    [MySqlFact]
    public async Task Cancelar_libera_el_turno_y_no_permite_cancelar_pasados()
    {
        var mani = await CrearManicuristaAsync();
        var horario = ProximoLunes.AddHours(9);

        await LeerAsync(await mani.Admin.PostAsJsonAsync("/api/admin/turnos/crear-manual", TurnoManual(horario)));
        var id = await TurnoIdAsync(mani.Admin, horario);

        Clock.Now = horario.AddHours(1);
        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await mani.Admin.PostAsync($"/api/admin/turnos/cancelar/{id}", null));

        Clock.Now = FakeClock.Inicial;
        await LeerAsync(await mani.Admin.PostAsync($"/api/admin/turnos/cancelar/{id}", null));

        var dia = await LeerAsync(await mani.Admin.GetAsync($"/api/admin/agenda?fecha={Fecha(ProximoLunes)}"));
        Assert.Equal("Disponible", dia.EnumerateArray().Single().GetProperty("estado").GetString());
    }

    [MySqlFact]
    public async Task Turno_manual_largo_bloquea_los_horarios_que_ocupa()
    {
        var mani = await CrearManicuristaAsync();
        var horario = ProximoLunes.AddHours(11);

        // 11:00 a 12:30
        await LeerAsync(await mani.Admin.PostAsJsonAsync(
            "/api/admin/turnos/crear-manual", TurnoManual(horario, 90)));

        var ocupados = (await DisponiblesAsync(mani.TenantId, ProximoLunes))
            .Where(s => s.GetProperty("estado").GetString() != "Disponible")
            .Select(s => s.GetProperty("fechaHora").GetDateTime())
            .ToList();
        Assert.Equal([horario, horario.AddMinutes(30), horario.AddMinutes(60)], ocupados);

        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, horario.AddMinutes(60)));
        await LeerAsync(await SolicitarAsync(mani.TenantId, horario.AddMinutes(90)));

        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await mani.Admin.PostAsJsonAsync("/api/admin/turnos/crear-manual", TurnoManual(horario.AddMinutes(45))));
    }

    [MySqlFact]
    public async Task Crear_manual_valida_datos()
    {
        var mani = await CrearManicuristaAsync();

        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await mani.Admin.PostAsJsonAsync("/api/admin/turnos/crear-manual", TurnoManual(Clock.Now.AddMinutes(-30))));

        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await mani.Admin.PostAsJsonAsync("/api/admin/turnos/crear-manual", TurnoManual(ProximoLunes.AddHours(10), 5)));

        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await mani.Admin.PostAsJsonAsync("/api/admin/turnos/crear-manual", new
            {
                fechaHora = FechaHora(ProximoLunes.AddHours(10)),
                nombreCliente = new string('x', 101),
                nota = ""
            }));
    }

    [MySqlFact]
    public async Task Reprogramar_mueve_el_turno_y_detecta_superposiciones()
    {
        var mani = await CrearManicuristaAsync();
        var a = ProximoLunes.AddHours(10);
        var b = ProximoLunes.AddHours(14);

        await LeerAsync(await mani.Admin.PostAsJsonAsync("/api/admin/turnos/crear-manual", TurnoManual(a)));
        await LeerAsync(await mani.Admin.PostAsJsonAsync("/api/admin/turnos/crear-manual", TurnoManual(b, 60)));
        var idA = await TurnoIdAsync(mani.Admin, a);

        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await mani.Admin.PutAsJsonAsync($"/api/admin/turnos/reprogramar/{idA}",
                new { fechaHora = FechaHora(b.AddMinutes(30)) }));

        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await mani.Admin.PutAsJsonAsync($"/api/admin/turnos/reprogramar/{idA}",
                new { fechaHora = FechaHora(Clock.Now.AddHours(-1)) }));

        var nuevo = ProximoLunes.AddHours(16).AddMinutes(15);
        var json = await LeerAsync(await mani.Admin.PutAsJsonAsync(
            $"/api/admin/turnos/reprogramar/{idA}", new { fechaHora = FechaHora(nuevo), duracionMinutos = 45 }));

        Assert.Equal(nuevo, json.GetProperty("fechaHora").GetDateTime());
        Assert.Equal(45, json.GetProperty("duracionMinutos").GetInt32());
    }

    [MySqlFact]
    public async Task Intercambiar_dos_turnos()
    {
        var mani = await CrearManicuristaAsync();
        var a = ProximoLunes.AddHours(10);
        var b = ProximoLunes.AddHours(15);

        await LeerAsync(await mani.Admin.PostAsJsonAsync("/api/admin/turnos/crear-manual", TurnoManual(a)));
        await LeerAsync(await SolicitarAsync(mani.TenantId, b, "Bea"));
        var idA = await TurnoIdAsync(mani.Admin, a);
        var idB = await TurnoIdAsync(mani.Admin, b);

        var json = await LeerAsync(await mani.Admin.PostAsJsonAsync(
            "/api/admin/turnos/intercambiar", new { turnoAId = idA, turnoBId = idB }));

        Assert.Equal(b, json.GetProperty("turnoA").GetProperty("fechaHora").GetDateTime());
        Assert.Equal(a, json.GetProperty("turnoB").GetProperty("fechaHora").GetDateTime());
        Assert.Equal(idB, await TurnoIdAsync(mani.Admin, a));
    }

    [MySqlFact]
    public async Task Vistas_de_agenda_historial_y_metricas_responden()
    {
        var mani = await CrearManicuristaAsync();

        // Sin turnos todavía (una manicurista recién dada de alta).
        await LeerAsync(await mani.Admin.GetAsync("/api/admin/metricas"));
        await LeerAsync(await mani.Admin.GetAsync("/api/admin/agenda/historial"));

        var horario = Clock.Now.Date.AddHours(16);
        await LeerAsync(await SolicitarAsync(mani.TenantId, horario));
        var id = await TurnoIdAsync(mani.Admin, horario);
        await LeerAsync(await mani.Admin.PostAsync($"/api/admin/turnos/confirmar/{id}", null));

        var slots = await LeerAsync(await mani.Admin.GetAsync($"/api/admin/agenda/slots?fecha={Fecha(horario)}"));
        Assert.False(slots.GetProperty("bloqueado").GetBoolean());
        Assert.Equal(18, slots.GetProperty("slots").GetArrayLength());

        var semana = await LeerAsync(await mani.Admin.GetAsync($"/api/admin/agenda/semana?fechaInicio={Fecha(Clock.Now)}"));
        Assert.Equal(7, semana.GetArrayLength());
        Assert.Equal(1, semana[0].GetProperty("turnos").GetArrayLength());

        // Al día siguiente el turno ya pasó: aparece en historial y métricas.
        Clock.Now = Clock.Now.AddDays(1);

        var historial = await LeerAsync(await mani.Admin.GetAsync("/api/admin/agenda/historial?pagina=0&tamano=1000"));
        Assert.Equal(id, Assert.Single(historial.EnumerateArray()).GetProperty("id").GetGuid());

        var metricas = await LeerAsync(await mani.Admin.GetAsync("/api/admin/metricas"));
        Assert.Equal(1, metricas.GetProperty("confirmados").GetInt32());
        Assert.Equal(100, metricas.GetProperty("tasaConfirmacion").GetDouble());
    }
}
