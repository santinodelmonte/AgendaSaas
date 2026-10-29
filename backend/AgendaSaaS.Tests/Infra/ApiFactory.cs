using AgendaSaaS.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Tests.Infra;

/// <summary>
/// Levanta la API completa (pipeline, middleware, auth, SQL real) contra una base
/// MySQL descartable que se crea al inicio y se borra al final.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SuperAdminEmail = "superadmin@tests.local";
    public const string SuperAdminPassword = "superadmin-tests-123";

    public static string? ServidorMySql =>
        Environment.GetEnvironmentVariable("AGENDASAAS_TEST_MYSQL");

    private readonly string _baseDeDatos =
        $"agendasaas_test_{Guid.NewGuid():N}"[..30];

    public FakeClock Clock { get; } = new();

    protected virtual int LoginPorMinuto => 10_000;

    public string ConnectionString =>
        $"{ServidorMySql!.TrimEnd(';')};Database={_baseDeDatos};Charset=utf8mb4;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Jwt:Key", "clave-de-tests-de-integracion-0123456789");
        builder.UseSetting("SuperAdmin:Email", SuperAdminEmail);
        builder.UseSetting("SuperAdmin:Password", SuperAdminPassword);
        builder.UseSetting("RateLimiting:LoginPorMinuto", LoginPorMinuto.ToString());
        builder.UseSetting("RateLimiting:ReservasPorHora", "10000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
    }

    public async Task InitializeAsync()
    {
        await EjecutarEnServidorAsync($"CREATE DATABASE {_baseDeDatos}");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        MySqlConnection.ClearAllPools();
        await EjecutarEnServidorAsync($"DROP DATABASE IF EXISTS {_baseDeDatos}");
    }

    private static async Task EjecutarEnServidorAsync(string sql)
    {
        if (string.IsNullOrWhiteSpace(ServidorMySql))
            return;

        using var connection = new MySqlConnection(ServidorMySql);
        await connection.OpenAsync();
        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Nombre)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Nombre = "api";
}
