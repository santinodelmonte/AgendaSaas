using System.Net;
using System.Net.Http.Json;
using AgendaSaaS.Tests.Infra;

namespace AgendaSaaS.Tests;

public class SuperAdminTests : ApiTestBase
{
    public SuperAdminTests(ApiFactory factory) : base(factory) { }

    [MySqlFact]
    public async Task Alta_genera_slug_unico_y_aparece_en_el_listado()
    {
        var nombre = $"Uñas Ñandú {Guid.NewGuid():N}"[..20];

        var primera = await CrearManicuristaAsync(nombre);
        var segunda = await CrearManicuristaAsync(nombre);

        Assert.Matches("^[a-z0-9-]+$", primera.Slug);
        Assert.StartsWith("unas-nandu-", primera.Slug);
        Assert.Equal($"{primera.Slug}-2", segunda.Slug);

        var listado = await LeerAsync(await (await SuperAdminAsync()).GetAsync("/api/superadmin/manicuristas"));
        Assert.Contains(listado.EnumerateArray(), m => m.GetProperty("tenantId").GetGuid() == primera.TenantId);
    }

    [MySqlFact]
    public async Task Alta_rechaza_email_duplicado_y_datos_invalidos()
    {
        var existente = await CrearManicuristaAsync();
        var superadmin = await SuperAdminAsync();

        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await superadmin.PostAsJsonAsync("/api/superadmin/manicuristas", new
            {
                nombre = "Otra",
                email = existente.Email.ToUpperInvariant(),
                whatsApp = "1",
                password = "password-123"
            }));

        await AssertStatusAsync(
            HttpStatusCode.BadRequest,
            await superadmin.PostAsJsonAsync("/api/superadmin/manicuristas", new
            {
                nombre = "Otra",
                email = "otra@tests.local",
                whatsApp = "1",
                password = "123"
            }));
    }

    [MySqlFact]
    public async Task Desactivar_corta_login_sesion_activa_y_reservas()
    {
        var mani = await CrearManicuristaAsync();
        var superadmin = await SuperAdminAsync();

        await LeerAsync(await superadmin.PutAsJsonAsync(
            $"/api/superadmin/manicuristas/{mani.TenantId}/activo", new { activo = false }));

        await AssertStatusAsync(HttpStatusCode.Unauthorized, await LoginAsync(mani.Email, mani.Password));
        await AssertStatusAsync(HttpStatusCode.Unauthorized, await mani.Admin.GetAsync("/api/admin/dashboard"));
        await AssertStatusAsync(HttpStatusCode.NotFound, await Anonimo().GetAsync($"/api/public/perfil/{mani.Slug}"));
        await AssertStatusAsync(HttpStatusCode.NotFound, await SolicitarAsync(mani.TenantId, ProximoLunes.AddHours(10)));

        await LeerAsync(await superadmin.PutAsJsonAsync(
            $"/api/superadmin/manicuristas/{mani.TenantId}/activo", new { activo = true }));

        await LeerAsync(await mani.Admin.GetAsync("/api/admin/dashboard"));
        await LeerAsync(await LoginAsync(mani.Email, mani.Password));
    }

    [MySqlFact]
    public async Task Reset_password_cambia_la_contrasena()
    {
        var mani = await CrearManicuristaAsync();
        var superadmin = await SuperAdminAsync();

        await LeerAsync(await superadmin.PostAsJsonAsync(
            $"/api/superadmin/manicuristas/{mani.TenantId}/reset-password", new { password = "nueva-clave-456" }));

        await AssertStatusAsync(HttpStatusCode.Unauthorized, await LoginAsync(mani.Email, mani.Password));
        await LeerAsync(await LoginAsync(mani.Email, "nueva-clave-456"));
    }

    [MySqlFact]
    public async Task Eliminar_borra_la_manicurista_y_sus_datos()
    {
        var mani = await CrearManicuristaAsync();
        await LeerAsync(await SolicitarAsync(mani.TenantId, ProximoLunes.AddHours(10)));
        var superadmin = await SuperAdminAsync();

        await LeerAsync(await superadmin.DeleteAsync($"/api/superadmin/manicuristas/{mani.TenantId}"));

        await AssertStatusAsync(HttpStatusCode.Unauthorized, await LoginAsync(mani.Email, mani.Password));
        await AssertStatusAsync(HttpStatusCode.Unauthorized, await mani.Admin.GetAsync("/api/admin/turnos/pendientes"));
        await AssertStatusAsync(HttpStatusCode.NotFound, await Anonimo().GetAsync($"/api/public/perfil/{mani.Slug}"));
    }
}
