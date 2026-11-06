using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Tests;

/// <summary>
/// Levanta la API contra una base MySQL temporal (se crea vacía y se borra al final).
/// Servidor configurable con la variable AGENDA_TEST_MYSQL
/// (default: MySQL local sin contraseña, como XAMPP).
/// </summary>
public class AgendaFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SuperEmail = "super@test.local";
    public const string SuperPassword = "super-password-test";

    private static readonly string Servidor =
        Environment.GetEnvironmentVariable("AGENDA_TEST_MYSQL")
        ?? "Server=localhost;Uid=root;Pwd=;";

    private readonly string _base = "agendasaas_test_" + Guid.NewGuid().ToString("N")[..8];

    private string ConnectionString => $"{Servidor.TrimEnd(';')};Database={_base};Charset=utf8mb4;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Jwt:Key", "clave-de-tests-suficientemente-larga-1234567890");
        builder.UseSetting("Jwt:Issuer", "AgendaSaaS");
        builder.UseSetting("Jwt:Audience", "AgendaSaaS");
        builder.UseSetting("Cors:Origins", "http://localhost");
        builder.UseSetting("SuperAdmin:Email", SuperEmail);
        builder.UseSetting("SuperAdmin:Password", SuperPassword);
        builder.UseSetting("RateLimits:LoginPorMinuto", "1000");
        builder.UseSetting("RateLimits:ReservasPor15Min", "1000");
    }

    public async Task InitializeAsync()
    {
        await using var conn = new MySqlConnection(Servidor);
        await conn.OpenAsync();
        await new MySqlCommand($"CREATE DATABASE `{_base}` CHARACTER SET utf8mb4", conn).ExecuteNonQueryAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await using var conn = new MySqlConnection(Servidor);
        await conn.OpenAsync();
        await new MySqlCommand($"DROP DATABASE IF EXISTS `{_base}`", conn).ExecuteNonQueryAsync();
    }
}
