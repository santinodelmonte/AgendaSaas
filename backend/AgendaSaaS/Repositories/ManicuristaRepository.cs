using AgendaSaaS.Data;
using AgendaSaaS.Entities;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Repositories;

public class ManicuristaRepository
    : IManicuristaRepository
{
    private readonly SqlConnectionFactory _factory;

    public ManicuristaRepository(
        SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Manicurista?> ObtenerPorTenantAsync(
        Guid tenantId)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            Nombre,
            Slug,
            Email,
            WhatsApp,
            Activo,
            DuracionTurnoMinutos
        FROM Manicuristas
        WHERE TenantId = @TenantId
        LIMIT 1
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@TenantId",
            tenantId.ToString());

        using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new Manicurista
        {
            Id = reader.GetGuid(0),
            TenantId = reader.GetGuid(1),
            Nombre = reader.GetString(2),
            Slug = reader.GetString(3),
            Email = reader.GetString(4),
            WhatsApp = reader.GetString(5),
            Activo = reader.GetBoolean(6),
            DuracionTurnoMinutos = reader.GetInt32(7)
        };
    }

    public async Task<Manicurista?> ObtenerPorSlugAsync(
        string slug)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            Nombre,
            Slug,
            Email,
            WhatsApp,
            Activo,
            DuracionTurnoMinutos
        FROM Manicuristas
        WHERE Slug = @Slug
        LIMIT 1
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Slug",
            slug);

        using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new Manicurista
        {
            Id = reader.GetGuid(0),
            TenantId = reader.GetGuid(1),
            Nombre = reader.GetString(2),
            Slug = reader.GetString(3),
            Email = reader.GetString(4),
            WhatsApp = reader.GetString(5),
            Activo = reader.GetBoolean(6),
            DuracionTurnoMinutos = reader.GetInt32(7)
        };
    }

    public async Task ActualizarAsync(
        Manicurista manicurista)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        UPDATE Manicuristas
        SET
            Nombre = @Nombre,
            Slug = @Slug,
            Email = @Email,
            WhatsApp = @WhatsApp,
            DuracionTurnoMinutos = @DuracionTurnoMinutos
        WHERE TenantId = @TenantId
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Nombre",
            manicurista.Nombre);

        command.Parameters.AddWithValue(
            "@Slug",
            manicurista.Slug);

        command.Parameters.AddWithValue(
            "@Email",
            manicurista.Email);

        command.Parameters.AddWithValue(
            "@WhatsApp",
            manicurista.WhatsApp);

        command.Parameters.AddWithValue(
            "@DuracionTurnoMinutos",
            manicurista.DuracionTurnoMinutos);

        command.Parameters.AddWithValue(
            "@TenantId",
            manicurista.TenantId.ToString());

        await command.ExecuteNonQueryAsync();
    }
}