using System.Net;
using AgendaSaaS.Tests.Infra;

namespace AgendaSaaS.Tests;

public class SaludYAuthTests : ApiTestBase
{
    public SaludYAuthTests(ApiFactory factory) : base(factory) { }

    [MySqlFact]
    public async Task Health_responde_ok_con_la_base_accesible()
    {
        var response = await Anonimo().GetAsync("/health");

        await AssertStatusAsync(HttpStatusCode.OK, response);
    }

    [MySqlFact]
    public async Task Login_con_credenciales_invalidas_devuelve_401()
    {
        await AssertStatusAsync(
            HttpStatusCode.Unauthorized,
            await LoginAsync(ApiFactory.SuperAdminEmail, "incorrecta"));

        await AssertStatusAsync(
            HttpStatusCode.Unauthorized,
            await LoginAsync("nadie@tests.local", "loquesea"));
    }

    [MySqlFact]
    public async Task Login_superadmin_devuelve_token_y_rol()
    {
        var json = await LeerAsync(
            await LoginAsync(ApiFactory.SuperAdminEmail, ApiFactory.SuperAdminPassword));

        Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
        Assert.Equal("SuperAdmin", json.GetProperty("rol").GetString());
    }

    [MySqlFact]
    public async Task Login_ignora_mayusculas_y_espacios_del_email()
    {
        var mani = await CrearManicuristaAsync();

        await LeerAsync(await LoginAsync($"  {mani.Email.ToUpperInvariant()} ", mani.Password));
    }

    [MySqlFact]
    public async Task Endpoints_de_admin_requieren_token()
    {
        await AssertStatusAsync(
            HttpStatusCode.Unauthorized,
            await Anonimo().GetAsync("/api/admin/dashboard"));

        await AssertStatusAsync(
            HttpStatusCode.Unauthorized,
            await Anonimo().GetAsync("/api/superadmin/manicuristas"));
    }

    [MySqlFact]
    public async Task Token_invalido_devuelve_401()
    {
        await AssertStatusAsync(
            HttpStatusCode.Unauthorized,
            await ConToken("esto.no.es-un-jwt").GetAsync("/api/admin/dashboard"));
    }

    [MySqlFact]
    public async Task Manicurista_no_puede_usar_endpoints_de_superadmin()
    {
        var mani = await CrearManicuristaAsync();

        await AssertStatusAsync(
            HttpStatusCode.Forbidden,
            await mani.Admin.GetAsync("/api/superadmin/manicuristas"));
    }
}

public class RateLimitFactory : ApiFactory
{
    protected override int LoginPorMinuto => 3;
}

public class RateLimitTests : IClassFixture<RateLimitFactory>
{
    private readonly RateLimitFactory _factory;

    public RateLimitTests(RateLimitFactory factory)
    {
        _factory = factory;
    }

    [MySqlFact]
    public async Task Login_se_bloquea_tras_demasiados_intentos()
    {
        var client = _factory.CreateClient();
        var estados = new List<HttpStatusCode>();

        for (var i = 0; i < 4; i++)
        {
            var response = await client.PostAsync(
                "/api/auth/login",
                System.Net.Http.Json.JsonContent.Create(new { email = "x@tests.local", password = "x" }));
            estados.Add(response.StatusCode);
        }

        Assert.Equal(
            [HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests],
            estados);
    }
}
