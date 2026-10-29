using System.Net;
using System.Net.Http.Json;
using AgendaSaaS.Tests.Infra;

namespace AgendaSaaS.Tests;

public class ConfiguracionTests : ApiTestBase
{
    public ConfiguracionTests(ApiFactory factory) : base(factory) { }

    [MySqlFact]
    public async Task Horario_con_pausa_no_ofrece_turnos_en_la_pausa()
    {
        var mani = await CrearManicuristaAsync();
        var horarios = await LeerAsync(await mani.Admin.GetAsync("/api/admin/horarios"));
        var lunes = horarios.EnumerateArray().Single(h => h.GetProperty("diaSemana").GetInt32() == 1);

        await LeerAsync(await mani.Admin.PutAsJsonAsync($"/api/admin/horarios/{lunes.GetProperty("id").GetGuid()}", new
        {
            diaSemana = 1,
            horaInicio = "09:00:00",
            horaFin = "13:00:00",
            pausaInicio = "11:00:00",
            pausaFin = "12:00:00"
        }));

        var slots = (await DisponiblesAsync(mani.TenantId, ProximoLunes))
            .Select(s => s.GetProperty("fechaHora").GetDateTime().TimeOfDay.TotalHours)
            .ToList();

        Assert.Equal([9, 9.5, 10, 10.5, 12, 12.5], slots);
        await AssertStatusAsync(HttpStatusCode.BadRequest, await SolicitarAsync(mani.TenantId, ProximoLunes.AddHours(11)));
    }

    [MySqlFact]
    public async Task Horarios_invalidos_o_duplicados_se_rechazan()
    {
        var mani = await CrearManicuristaAsync();

        // Inicio después del fin.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await mani.Admin.PostAsJsonAsync("/api/admin/horarios",
            new { diaSemana = 6, horaInicio = "18:00:00", horaFin = "09:00:00" }));
        // Pausa fuera del horario.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await mani.Admin.PostAsJsonAsync("/api/admin/horarios",
            new { diaSemana = 6, horaInicio = "09:00:00", horaFin = "13:00:00", pausaInicio = "14:00:00", pausaFin = "15:00:00" }));
        // El lunes ya tiene horario por defecto.
        await AssertStatusAsync(HttpStatusCode.BadRequest, await mani.Admin.PostAsJsonAsync("/api/admin/horarios",
            new { diaSemana = 1, horaInicio = "09:00:00", horaFin = "13:00:00" }));

        // Sábado nuevo: válido.
        await LeerAsync(await mani.Admin.PostAsJsonAsync("/api/admin/horarios",
            new { diaSemana = 6, horaInicio = "09:00:00", horaFin = "12:00:00" }));
        Assert.Equal(6, (await DisponiblesAsync(mani.TenantId, ProximoLunes.AddDays(5))).Count);
    }

    [MySqlFact]
    public async Task Perfil_cambia_la_duracion_de_los_turnos()
    {
        var mani = await CrearManicuristaAsync();
        var perfil = await LeerAsync(await mani.Admin.GetAsync("/api/admin/perfil"));

        await LeerAsync(await mani.Admin.PutAsJsonAsync("/api/admin/perfil", new
        {
            nombre = perfil.GetProperty("nombre").GetString(),
            email = perfil.GetProperty("email").GetString(),
            whatsApp = "+5491199999999",
            duracionTurnoMinutos = 60
        }));

        Assert.Equal(9, (await DisponiblesAsync(mani.TenantId, ProximoLunes)).Count);

        await AssertStatusAsync(HttpStatusCode.BadRequest, await mani.Admin.PutAsJsonAsync("/api/admin/perfil", new
        {
            nombre = "x",
            email = "x@tests.local",
            whatsApp = "1",
            duracionTurnoMinutos = 0
        }));
    }

    [MySqlFact]
    public async Task No_se_pueden_bloquear_fechas_pasadas_ni_repetidas()
    {
        var mani = await CrearManicuristaAsync();

        await AssertStatusAsync(HttpStatusCode.BadRequest, await mani.Admin.PostAsJsonAsync(
            "/api/admin/dias-bloqueados", new { fecha = Fecha(Clock.Now.AddDays(-1)) }));

        await LeerAsync(await mani.Admin.PostAsJsonAsync(
            "/api/admin/dias-bloqueados", new { fecha = Fecha(Clock.Now) }));

        await AssertStatusAsync(HttpStatusCode.BadRequest, await mani.Admin.PostAsJsonAsync(
            "/api/admin/dias-bloqueados", new { fecha = Fecha(Clock.Now) }));
    }
}
