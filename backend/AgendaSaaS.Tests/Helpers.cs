using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgendaSaaS.Tests;

public record Manicurista(Guid TenantId, string Slug, string Email, string Password);

public static class Helpers
{
    public static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    public static HttpClient ConToken(this AgendaFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient Publico(this AgendaFactory factory, Guid tenantId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }

    public static async Task<Manicurista> CrearManicuristaAsync(AgendaFactory factory, string nombre)
    {
        var superToken = await LoginAsync(factory.CreateClient(), AgendaFactory.SuperEmail, AgendaFactory.SuperPassword);
        var email = $"{nombre.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        const string password = "password-123";

        var resp = await factory.ConToken(superToken).PostAsJsonAsync("/api/superadmin/manicuristas", new
        {
            nombre,
            email,
            whatsApp = "099123456",
            duracionTurnoMinutos = 60,
            password
        });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();

        return new Manicurista(
            json.GetProperty("tenantId").GetGuid(),
            json.GetProperty("slug").GetString()!,
            email,
            password);
    }

    /// <summary>Próximo día hábil (horario por defecto: lun-vie 9 a 18) a 3+ días de hoy.</summary>
    public static DateTime ProximoHabil(int hora, int diasMinimos = 3)
    {
        var dia = AgendaSaaS.Services.HoraLocal.Hoy.AddDays(diasMinimos);
        while (dia.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            dia = dia.AddDays(1);
        return dia.AddHours(hora);
    }

    public static object Reserva(DateTime fechaHora, string nombre = "Clienta Test") => new
    {
        fechaHora = fechaHora.ToString("yyyy-MM-ddTHH:mm:ss"),
        nombreCliente = nombre,
        telefonoCliente = "099000111",
        servicio = "Esmaltado"
    };
}
