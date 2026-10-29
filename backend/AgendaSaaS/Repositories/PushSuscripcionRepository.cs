using System.Security.Cryptography;
using System.Text;
using AgendaSaaS.Data;
using AgendaSaaS.Entities;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Repositories;

public class PushSuscripcionRepository : IPushSuscripcionRepository
{
    private readonly SqlConnectionFactory _factory;

    public PushSuscripcionRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task GuardarAsync(PushSuscripcion suscripcion)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        // El endpoint identifica al dispositivo: si vuelve a suscribirse (o lo usa
        // otra cuenta en el mismo navegador) se actualiza en vez de duplicarse.
        var sql =
        """
        INSERT INTO PushSuscripciones
            (Id, TenantId, EndpointHash, Endpoint, P256dh, Auth, FechaCreacion)
        VALUES
            (@Id, @TenantId, @EndpointHash, @Endpoint, @P256dh, @Auth, @FechaCreacion)
        ON DUPLICATE KEY UPDATE
            TenantId = VALUES(TenantId),
            P256dh = VALUES(P256dh),
            Auth = VALUES(Auth)
        """;

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", suscripcion.Id.ToString());
        command.Parameters.AddWithValue("@TenantId", suscripcion.TenantId.ToString());
        command.Parameters.AddWithValue("@EndpointHash", Hash(suscripcion.Endpoint));
        command.Parameters.AddWithValue("@Endpoint", suscripcion.Endpoint);
        command.Parameters.AddWithValue("@P256dh", suscripcion.P256dh);
        command.Parameters.AddWithValue("@Auth", suscripcion.Auth);
        command.Parameters.AddWithValue("@FechaCreacion", suscripcion.FechaCreacion);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<PushSuscripcion>> ObtenerPorTenantAsync(Guid tenantId)
    {
        var resultado = new List<PushSuscripcion>();

        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var sql =
        """
        SELECT Id, TenantId, Endpoint, P256dh, Auth, FechaCreacion
        FROM PushSuscripciones
        WHERE TenantId = @TenantId
        """;

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TenantId", tenantId.ToString());

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(new PushSuscripcion
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1),
                Endpoint = reader.GetString(2),
                P256dh = reader.GetString(3),
                Auth = reader.GetString(4),
                FechaCreacion = reader.GetDateTime(5)
            });
        }

        return resultado;
    }

    public async Task<bool> EliminarAsync(Guid tenantId, string endpoint)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var sql = "DELETE FROM PushSuscripciones WHERE EndpointHash = @EndpointHash AND TenantId = @TenantId";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EndpointHash", Hash(endpoint));
        command.Parameters.AddWithValue("@TenantId", tenantId.ToString());

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task EliminarPorIdAsync(Guid id)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        using var command = new MySqlCommand("DELETE FROM PushSuscripciones WHERE Id = @Id", connection);
        command.Parameters.AddWithValue("@Id", id.ToString());

        await command.ExecuteNonQueryAsync();
    }

    // Los endpoints son URLs largas: se indexan por su SHA-256.
    private static string Hash(string endpoint) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint)));
}
