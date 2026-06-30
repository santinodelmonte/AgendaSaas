using AgendaSaaS.Data;
using AgendaSaaS.Entities;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Repositories;

public class BloqueRepository
    : IBloqueRepository
{
    private readonly SqlConnectionFactory _factory;

    public BloqueRepository(
        SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<BloqueHorarioConfig>>
        ObtenerPorTenantAsync(Guid tenantId)
    {
        var resultado =
            new List<BloqueHorarioConfig>();

        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            DiaSemana,
            HoraInicio,
            HoraFin,
            PausaInicio,
            PausaFin
        FROM BloqueHorarioConfig
        WHERE TenantId = @TenantId
        ORDER BY
            DiaSemana,
            CASE WHEN PausaInicio IS NULL THEN 1 ELSE 0 END,
            HoraInicio
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@TenantId",
            tenantId.ToString());

        using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(
                new BloqueHorarioConfig
                {
                    Id = reader.GetGuid(0),

                    TenantId = reader.GetGuid(1),

                    DiaSemana =
                        (DayOfWeek)
                        reader.GetInt32(2),

                    HoraInicio =
                        (TimeSpan)reader.GetValue(3),

                    HoraFin =
                        (TimeSpan)reader.GetValue(4),

                    PausaInicio =
                        reader.IsDBNull(5)
                            ? null
                            : (TimeSpan)reader.GetValue(5),

                    PausaFin =
                        reader.IsDBNull(6)
                            ? null
                            : (TimeSpan)reader.GetValue(6)
                });
        }

        return resultado;
    }

    public async Task<BloqueHorarioConfig?>
        ObtenerPorIdAsync(Guid id)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            DiaSemana,
            HoraInicio,
            HoraFin,
            PausaInicio,
            PausaFin
        FROM BloqueHorarioConfig
        WHERE Id = @Id
        LIMIT 1
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Id",
            id.ToString());

        using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new BloqueHorarioConfig
        {
            Id = reader.GetGuid(0),

            TenantId = reader.GetGuid(1),

            DiaSemana =
                (DayOfWeek)
                reader.GetInt32(2),

            HoraInicio =
                (TimeSpan)reader.GetValue(3),

            HoraFin =
                (TimeSpan)reader.GetValue(4),

            PausaInicio =
                reader.IsDBNull(5)
                    ? null
                    : (TimeSpan)reader.GetValue(5),

            PausaFin =
                reader.IsDBNull(6)
                    ? null
                    : (TimeSpan)reader.GetValue(6)
        };
    }

    public async Task CrearAsync(
        BloqueHorarioConfig bloque)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        INSERT INTO BloqueHorarioConfig
        (
            Id,
            TenantId,
            DiaSemana,
            HoraInicio,
            HoraFin,
            PausaInicio,
            PausaFin
        )
        VALUES
        (
            @Id,
            @TenantId,
            @DiaSemana,
            @HoraInicio,
            @HoraFin,
            @PausaInicio,
            @PausaFin
        )
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Id",
            bloque.Id.ToString());

        command.Parameters.AddWithValue(
            "@TenantId",
            bloque.TenantId.ToString());

        command.Parameters.AddWithValue(
            "@DiaSemana",
            (int)bloque.DiaSemana);

        command.Parameters.AddWithValue(
            "@HoraInicio",
            bloque.HoraInicio);

        command.Parameters.AddWithValue(
            "@HoraFin",
            bloque.HoraFin);

        command.Parameters.AddWithValue(
            "@PausaInicio",
            (object?)bloque.PausaInicio
            ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@PausaFin",
            (object?)bloque.PausaFin
            ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task ActualizarAsync(
        BloqueHorarioConfig bloque)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        UPDATE BloqueHorarioConfig
        SET
            DiaSemana = @DiaSemana,
            HoraInicio = @HoraInicio,
            HoraFin = @HoraFin,
            PausaInicio = @PausaInicio,
            PausaFin = @PausaFin
        WHERE Id = @Id
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Id",
            bloque.Id.ToString());

        command.Parameters.AddWithValue(
            "@DiaSemana",
            (int)bloque.DiaSemana);

        command.Parameters.AddWithValue(
            "@HoraInicio",
            bloque.HoraInicio);

        command.Parameters.AddWithValue(
            "@HoraFin",
            bloque.HoraFin);

        command.Parameters.AddWithValue(
            "@PausaInicio",
            (object?)bloque.PausaInicio
            ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@PausaFin",
            (object?)bloque.PausaFin
            ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task EliminarAsync(
        Guid id)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        DELETE FROM BloqueHorarioConfig
        WHERE Id = @Id
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Id",
            id.ToString());

        await command.ExecuteNonQueryAsync();
    }
}