using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgendaSaaS.Tests.Infra;

public record ManicuristaDePrueba(
    Guid TenantId,
    string Slug,
    string Email,
    string Password,
    HttpClient Admin);

[Collection(ApiCollection.Nombre)]
public abstract class ApiTestBase
{
    protected ApiTestBase(ApiFactory factory)
    {
        Factory = factory;
        Factory.Clock.Now = FakeClock.Inicial;
    }

    protected ApiFactory Factory { get; }

    protected FakeClock Clock => Factory.Clock;

    // Lunes de la semana siguiente al "ahora" de los tests: todos sus horarios
    // están fuera de la anticipación mínima.
    protected static DateTime ProximoLunes => FakeClock.Inicial.Date.AddDays(7);

    protected static string Fecha(DateTime d) => d.ToString("yyyy-MM-dd");

    protected static string FechaHora(DateTime d) => d.ToString("yyyy-MM-ddTHH:mm:ss");

    protected HttpClient Anonimo() => Factory.CreateClient();

    protected HttpClient ConToken(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected HttpClient Publico(Guid tenantId)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    protected async Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        await Anonimo().PostAsJsonAsync("/api/auth/login", new { email, password });

    protected async Task<string> TokenAsync(string email, string password)
    {
        var response = await LoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await LeerAsync(response);
        return json.GetProperty("token").GetString()!;
    }

    protected async Task<HttpClient> SuperAdminAsync() =>
        ConToken(await TokenAsync(ApiFactory.SuperAdminEmail, ApiFactory.SuperAdminPassword));

    protected async Task<ManicuristaDePrueba> CrearManicuristaAsync(
        string? nombre = null,
        int duracion = 30)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"mani-{id}@tests.local";
        var password = "password-123";

        var superadmin = await SuperAdminAsync();

        var response = await superadmin.PostAsJsonAsync("/api/superadmin/manicuristas", new
        {
            nombre = nombre ?? $"Manicurista {id}",
            email,
            whatsApp = "+5491100000000",
            duracionTurnoMinutos = duracion,
            password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await LeerAsync(response);

        var token = await TokenAsync(email, password);

        return new ManicuristaDePrueba(
            json.GetProperty("tenantId").GetGuid(),
            json.GetProperty("slug").GetString()!,
            email,
            password,
            ConToken(token));
    }

    protected async Task<HttpResponseMessage> SolicitarAsync(
        Guid tenantId,
        DateTime fechaHora,
        string nombre = "Clienta Test")
    {
        return await Publico(tenantId).PostAsJsonAsync("/api/turnos/solicitar", new
        {
            fechaHora = FechaHora(fechaHora),
            nombreCliente = nombre,
            telefonoCliente = "1122334455",
            servicio = "Semipermanente"
        });
    }

    protected async Task<Guid> TurnoIdAsync(HttpClient admin, DateTime fechaHora)
    {
        var dia = await LeerAsync(
            await admin.GetAsync($"/api/admin/agenda?fecha={Fecha(fechaHora)}"));

        return dia.EnumerateArray()
            .Single(t => t.GetProperty("fechaHora").GetDateTime() == fechaHora)
            .GetProperty("id").GetGuid();
    }

    protected async Task<List<JsonElement>> DisponiblesAsync(Guid tenantId, DateTime fecha)
    {
        var response = await Publico(tenantId)
            .GetAsync($"/api/turnos/disponibles?fecha={Fecha(fecha)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await LeerAsync(response)).EnumerateArray().ToList();
    }

    protected static async Task<JsonElement> LeerAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.IsSuccessStatusCode,
            $"Se esperaba éxito y llegó {(int)response.StatusCode}: {body}");
        return string.IsNullOrEmpty(body)
            ? default
            : JsonDocument.Parse(body).RootElement.Clone();
    }

    protected static async Task AssertStatusAsync(
        HttpStatusCode esperado,
        HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            esperado == response.StatusCode,
            $"Se esperaba {(int)esperado} y llegó {(int)response.StatusCode}: {body}");
    }
}
