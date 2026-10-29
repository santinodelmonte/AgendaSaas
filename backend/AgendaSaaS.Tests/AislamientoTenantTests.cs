using System.Net;
using System.Net.Http.Json;
using AgendaSaaS.Tests.Infra;

namespace AgendaSaaS.Tests;

/// <summary>
/// Una manicurista autenticada nunca puede ver ni modificar datos de otra.
/// </summary>
public class AislamientoTenantTests : ApiTestBase
{
    public AislamientoTenantTests(ApiFactory factory) : base(factory) { }

    [MySqlFact]
    public async Task No_puede_operar_turnos_de_otra_manicurista()
    {
        var a = await CrearManicuristaAsync();
        var b = await CrearManicuristaAsync();
        var horario = ProximoLunes.AddHours(10);

        await LeerAsync(await SolicitarAsync(a.TenantId, horario));
        var id = await TurnoIdAsync(a.Admin, horario);

        await AssertStatusAsync(HttpStatusCode.NotFound, await b.Admin.PostAsync($"/api/admin/turnos/confirmar/{id}", null));
        await AssertStatusAsync(HttpStatusCode.NotFound, await b.Admin.PostAsync($"/api/admin/turnos/rechazar/{id}", null));
        await AssertStatusAsync(HttpStatusCode.NotFound, await b.Admin.PostAsync($"/api/admin/turnos/cancelar/{id}", null));
        await AssertStatusAsync(
            HttpStatusCode.NotFound,
            await b.Admin.PutAsJsonAsync($"/api/admin/turnos/reprogramar/{id}", new { fechaHora = FechaHora(horario.AddHours(1)) }));

        var pendientesB = await LeerAsync(await b.Admin.GetAsync("/api/admin/turnos/pendientes"));
        Assert.Equal(0, pendientesB.GetArrayLength());

        var diaB = await LeerAsync(await b.Admin.GetAsync($"/api/admin/agenda?fecha={Fecha(ProximoLunes)}"));
        Assert.Equal(0, diaB.GetArrayLength());
    }

    [MySqlFact]
    public async Task El_header_X_Tenant_Id_se_ignora_si_hay_token()
    {
        var a = await CrearManicuristaAsync();
        var b = await CrearManicuristaAsync();

        await LeerAsync(await SolicitarAsync(a.TenantId, ProximoLunes.AddHours(10)));

        b.Admin.DefaultRequestHeaders.Add("X-Tenant-Id", a.TenantId.ToString());
        var pendientes = await LeerAsync(await b.Admin.GetAsync("/api/admin/turnos/pendientes"));

        Assert.Equal(0, pendientes.GetArrayLength());
    }

    [MySqlFact]
    public async Task No_puede_borrar_dias_bloqueados_de_otra_manicurista()
    {
        var a = await CrearManicuristaAsync();
        var b = await CrearManicuristaAsync();

        var dia = await LeerAsync(await a.Admin.PostAsJsonAsync(
            "/api/admin/dias-bloqueados", new { fecha = Fecha(ProximoLunes), motivo = "Vacaciones" }));
        var id = dia.GetProperty("id").GetGuid();

        await AssertStatusAsync(HttpStatusCode.NotFound, await b.Admin.DeleteAsync($"/api/admin/dias-bloqueados/{id}"));

        var diasA = await LeerAsync(await a.Admin.GetAsync("/api/admin/dias-bloqueados"));
        Assert.Equal(1, diasA.GetArrayLength());

        await LeerAsync(await a.Admin.DeleteAsync($"/api/admin/dias-bloqueados/{id}"));
    }

    [MySqlFact]
    public async Task No_puede_modificar_servicios_ni_horarios_de_otra_manicurista()
    {
        var a = await CrearManicuristaAsync();
        var b = await CrearManicuristaAsync();

        var servicio = await LeerAsync(await a.Admin.PostAsJsonAsync(
            "/api/admin/servicios", new { nombre = "Kapping", precio = 100m }));
        var servicioId = servicio.GetProperty("id").GetGuid();

        await AssertStatusAsync(
            HttpStatusCode.NotFound,
            await b.Admin.PutAsJsonAsync($"/api/admin/servicios/{servicioId}", new { nombre = "Hackeado", precio = 0m, activo = true }));
        await AssertStatusAsync(HttpStatusCode.NotFound, await b.Admin.DeleteAsync($"/api/admin/servicios/{servicioId}"));

        var horariosA = await LeerAsync(await a.Admin.GetAsync("/api/admin/horarios"));
        var horarioId = horariosA[0].GetProperty("id").GetGuid();

        await AssertStatusAsync(HttpStatusCode.NotFound, await b.Admin.DeleteAsync($"/api/admin/horarios/{horarioId}"));
        await AssertStatusAsync(
            HttpStatusCode.NotFound,
            await b.Admin.PutAsJsonAsync($"/api/admin/horarios/{horarioId}",
                new { diaSemana = 1, horaInicio = "10:00:00", horaFin = "11:00:00" }));
    }
}
