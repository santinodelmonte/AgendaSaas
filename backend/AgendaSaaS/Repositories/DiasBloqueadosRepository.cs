using AgendaSaaS.Data;
using AgendaSaaS.Entities;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Repositories;

public class DiasBloqueadosRepository : IDiasBloqueadosRepository
{
    private readonly SqlConnectionFactory _factory;

    public DiasBloqueadosRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<DiasBloqueado>> ObtenerPorTenantAsync(Guid tenantId)
    {
        var resultado = new List<DiasBloqueado>();

        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var sql =
        """
        SELECT Id, TenantId, Fecha, Motivo
        FROM DiasBloqueados
        WHERE TenantId = @TenantId
        ORDER BY Fecha
        """;

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TenantId", tenantId.ToString());

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(new DiasBloqueado
            {
                Id = reader.GetGuid(0),
                TenantId = reader.GetGuid(1),
                Fecha = reader.GetDateTime(2),
                Motivo = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return resultado;
    }

    public async Task<DiasBloqueado?> ObtenerPorFechaAsync(Guid tenantId, DateTime fecha)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var sql =
        """
        SELECT Id, TenantId, Fecha, Motivo
        FROM DiasBloqueados
        WHERE TenantId = @TenantId AND Fecha = @Fecha
        LIMIT 1
        """;

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TenantId", tenantId.ToString());
        command.Parameters.AddWithValue("@Fecha", fecha.Date);

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return null;

        return new DiasBloqueado
        {
            Id = reader.GetGuid(0),
            TenantId = reader.GetGuid(1),
            Fecha = reader.GetDateTime(2),
            Motivo = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }

    public async Task CrearAsync(DiasBloqueado dia)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var sql =
        """
        INSERT INTO DiasBloqueados (Id, TenantId, Fecha, Motivo)
        VALUES (@Id, @TenantId, @Fecha, @Motivo)
        """;

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", dia.Id.ToString());
        command.Parameters.AddWithValue("@TenantId", dia.TenantId.ToString());
        command.Parameters.AddWithValue("@Fecha", dia.Fecha.Date);
        command.Parameters.AddWithValue("@Motivo", (object?)dia.Motivo ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task EliminarAsync(Guid id)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var sql = "DELETE FROM DiasBloqueados WHERE Id = @Id";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());

        await command.ExecuteNonQueryAsync();
    }
}
