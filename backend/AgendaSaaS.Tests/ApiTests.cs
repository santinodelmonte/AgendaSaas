using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static AgendaSaaS.Tests.Helpers;

namespace AgendaSaaS.Tests;

public class ApiTests : IClassFixture<AgendaFactory>
{
    private readonly AgendaFactory _factory;

    public ApiTests(AgendaFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_responde_ok_con_base()
    {
        var resp = await _factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Login_con_password_incorrecta_da_401()
    {
        var m = await CrearManicuristaAsync(_factory, "Login");
        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = m.Email, password = "incorrecta" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Manicurista_no_puede_usar_endpoints_de_superadmin()
    {
        var m = await CrearManicuristaAsync(_factory, "Rol");
        var token = await LoginAsync(_factory.CreateClient(), m.Email, m.Password);
        var resp = await _factory.ConToken(token).GetAsync("/api/superadmin/manicuristas");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Endpoint_admin_sin_token_es_rechazado()
    {
        var m = await CrearManicuristaAsync(_factory, "SinToken");
        var resp = await _factory.Publico(m.TenantId).GetAsync("/api/admin/turnos/pendientes");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Header_de_tenant_forjado_no_expone_datos_de_otra_manicurista()
    {
        var a = await CrearManicuristaAsync(_factory, "Ana");
        var b = await CrearManicuristaAsync(_factory, "Bea");

        var reserva = await _factory.Publico(b.TenantId)
            .PostAsJsonAsync("/api/turnos/solicitar", Reserva(ProximoHabil(10), "Secreto De Bea"));
        Assert.Equal(HttpStatusCode.OK, reserva.StatusCode);

        var tokenA = await LoginAsync(_factory.CreateClient(), a.Email, a.Password);
        var clienteA = _factory.ConToken(tokenA);
        clienteA.DefaultRequestHeaders.Add("X-Tenant-Id", b.TenantId.ToString());

        var body = await clienteA.GetStringAsync("/api/admin/turnos/pendientes");
        Assert.DoesNotContain("Secreto De Bea", body);

        var tokenB = await LoginAsync(_factory.CreateClient(), b.Email, b.Password);
        var bodyB = await _factory.ConToken(tokenB).GetStringAsync("/api/admin/turnos/pendientes");
        Assert.Contains("Secreto De Bea", bodyB);
    }

    [Fact]
    public async Task Reserva_publica_sin_header_de_tenant_da_400()
    {
        var resp = await _factory.CreateClient()
            .PostAsJsonAsync("/api/turnos/solicitar", Reserva(ProximoHabil(10)));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reserva_ok_y_el_horario_queda_ocupado()
    {
        var m = await CrearManicuristaAsync(_factory, "Ocupado");
        var publico = _factory.Publico(m.TenantId);
        var fecha = ProximoHabil(11);

        Assert.Equal(HttpStatusCode.OK,
            (await publico.PostAsJsonAsync("/api/turnos/solicitar", Reserva(fecha))).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await publico.PostAsJsonAsync("/api/turnos/solicitar", Reserva(fecha, "Otra"))).StatusCode);

        var slots = await publico.GetFromJsonAsync<JsonElement>(
            $"/api/turnos/disponibles?fecha={fecha:yyyy-MM-dd}");
        var slot = slots.EnumerateArray()
            .Single(s => s.GetProperty("fechaHora").GetDateTime() == fecha);
        Assert.Equal("Pendiente", slot.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Reservas_simultaneas_del_mismo_horario_solo_una_gana()
    {
        var m = await CrearManicuristaAsync(_factory, "Carrera");
        var fecha = ProximoHabil(14);

        var intentos = Enumerable.Range(0, 8).Select(i =>
            _factory.Publico(m.TenantId)
                .PostAsJsonAsync("/api/turnos/solicitar", Reserva(fecha, $"Clienta {i}")));

        var resultados = await Task.WhenAll(intentos);

        Assert.Equal(1, resultados.Count(r => r.StatusCode == HttpStatusCode.OK));
    }

    [Fact]
    public async Task Reserva_sin_2_horas_de_anticipacion_da_400()
    {
        var m = await CrearManicuristaAsync(_factory, "Pronto");
        var resp = await _factory.Publico(m.TenantId)
            .PostAsJsonAsync("/api/turnos/solicitar", Reserva(AgendaSaaS.Services.HoraLocal.Ahora.AddMinutes(30)));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reserva_fuera_de_horario_da_400()
    {
        var m = await CrearManicuristaAsync(_factory, "Horario");
        var resp = await _factory.Publico(m.TenantId)
            .PostAsJsonAsync("/api/turnos/solicitar", Reserva(ProximoHabil(20)));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Desactivar_manicurista_invalida_su_token_al_instante()
    {
        var m = await CrearManicuristaAsync(_factory, "Baja");
        var token = await LoginAsync(_factory.CreateClient(), m.Email, m.Password);
        var cliente = _factory.ConToken(token);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/admin/turnos/pendientes")).StatusCode);

        var superToken = await LoginAsync(_factory.CreateClient(), AgendaFactory.SuperEmail, AgendaFactory.SuperPassword);
        var baja = await _factory.ConToken(superToken)
            .PutAsJsonAsync($"/api/superadmin/manicuristas/{m.TenantId}/activo", new { activo = false });
        baja.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/admin/turnos/pendientes")).StatusCode);
    }

    [Fact]
    public async Task Confirmar_turno_de_otra_manicurista_da_404()
    {
        var a = await CrearManicuristaAsync(_factory, "Duena");
        var b = await CrearManicuristaAsync(_factory, "Intrusa");

        await _factory.Publico(a.TenantId)
            .PostAsJsonAsync("/api/turnos/solicitar", Reserva(ProximoHabil(15)));

        var tokenA = await LoginAsync(_factory.CreateClient(), a.Email, a.Password);
        var pendientes = await _factory.ConToken(tokenA)
            .GetFromJsonAsync<JsonElement>("/api/admin/turnos/pendientes");
        var id = pendientes.EnumerateArray().First().GetProperty("id").GetGuid();

        var tokenB = await LoginAsync(_factory.CreateClient(), b.Email, b.Password);
        var resp = await _factory.ConToken(tokenB).PostAsync($"/api/admin/turnos/confirmar/{id}", null);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
