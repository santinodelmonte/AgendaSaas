using AgendaSaaS.Data;
using AgendaSaaS.Entities;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Repositories;

public class TurnoRepository : ITurnoRepository
{
    private readonly SqlConnectionFactory _factory;

    public TurnoRepository(
        SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<Turno>> ObtenerPorFechaAsync(
        Guid tenantId,
        DateTime fecha)
    {
        var resultado = new List<Turno>();

        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var inicioDia = fecha.Date;
        var finDia = inicioDia.AddDays(1);

        var sql =
        """
        SELECT
            Id,
            TenantId,
            FechaHora,
            NombreCliente,
            TelefonoCliente,
            ServicioSolicitado,
            NotaInterna,
            Estado,
            FechaCreacionPendiente,
            Version
        FROM Turnos
        WHERE TenantId = @TenantId
          AND FechaHora >= @Inicio
          AND FechaHora < @Fin
        ORDER BY FechaHora
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@TenantId",
            tenantId.ToString());

        command.Parameters.AddWithValue(
            "@Inicio",
            inicioDia);

        command.Parameters.AddWithValue(
            "@Fin",
            finDia);

        using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(
                MapearTurno(reader));
        }

        return resultado;
    }

    public async Task<Turno?> ObtenerPorFechaHoraAsync(
        Guid tenantId,
        DateTime fechaHora)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            FechaHora,
            NombreCliente,
            TelefonoCliente,
            ServicioSolicitado,
            NotaInterna,
            Estado,
            FechaCreacionPendiente,
            Version
        FROM Turnos
        WHERE TenantId = @TenantId
          AND FechaHora = @FechaHora
        LIMIT 1
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@TenantId",
            tenantId.ToString());

        command.Parameters.AddWithValue(
            "@FechaHora",
            fechaHora);

        using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return MapearTurno(reader);
    }

    public async Task<Turno?> ObtenerPorIdAsync(
        Guid id)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            FechaHora,
            NombreCliente,
            TelefonoCliente,
            ServicioSolicitado,
            NotaInterna,
            Estado,
            FechaCreacionPendiente,
            Version
        FROM Turnos
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

        return MapearTurno(reader);
    }

    public async Task<List<Turno>> ObtenerPendientesAsync(
        Guid tenantId)
    {
        var resultado = new List<Turno>();

        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            FechaHora,
            NombreCliente,
            TelefonoCliente,
            ServicioSolicitado,
            NotaInterna,
            Estado,
            FechaCreacionPendiente,
            Version
        FROM Turnos
        WHERE TenantId = @TenantId
          AND Estado = @Estado
        ORDER BY FechaHora
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@TenantId",
            tenantId.ToString());

        command.Parameters.AddWithValue(
            "@Estado",
            (int)TurnoEstado.Pendiente);

        using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(
                MapearTurno(reader));
        }

        return resultado;
    }

    public async Task<int> ContarPendientesAsync(
        Guid tenantId)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT COUNT(*)
        FROM Turnos
        WHERE TenantId = @TenantId
          AND Estado = @Estado
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@TenantId",
            tenantId.ToString());

        command.Parameters.AddWithValue(
            "@Estado",
            (int)TurnoEstado.Pendiente);

        var result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    public async Task<int> ContarConfirmadosEntreFechasAsync(
        Guid tenantId,
        DateTime inicio,
        DateTime fin)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT COUNT(*)
        FROM Turnos
        WHERE TenantId = @TenantId
          AND Estado = @Estado
          AND FechaHora >= @Inicio
          AND FechaHora < @Fin
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@TenantId",
            tenantId.ToString());

        command.Parameters.AddWithValue(
            "@Estado",
            (int)TurnoEstado.Confirmado);

        command.Parameters.AddWithValue(
            "@Inicio",
            inicio);

        command.Parameters.AddWithValue(
            "@Fin",
            fin);

        var result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    public async Task<Turno?> ObtenerProximoConfirmadoAsync(
        Guid tenantId,
        DateTime desde)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            FechaHora,
            NombreCliente,
            TelefonoCliente,
            ServicioSolicitado,
            NotaInterna,
            Estado,
            FechaCreacionPendiente,
            Version
        FROM Turnos
        WHERE TenantId = @TenantId
          AND Estado = @Estado
          AND FechaHora >= @Desde
        ORDER BY FechaHora
        LIMIT 1
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@TenantId",
            tenantId.ToString());

        command.Parameters.AddWithValue(
            "@Estado",
            (int)TurnoEstado.Confirmado);

        command.Parameters.AddWithValue(
            "@Desde",
            desde);

        using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return MapearTurno(reader);
    }

    public async Task CrearAsync(
        Turno turno)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        INSERT INTO Turnos
        (
            Id,
            TenantId,
            FechaHora,
            NombreCliente,
            TelefonoCliente,
            ServicioSolicitado,
            NotaInterna,
            Estado,
            FechaCreacionPendiente,
            Version
        )
        VALUES
        (
            @Id,
            @TenantId,
            @FechaHora,
            @NombreCliente,
            @TelefonoCliente,
            @ServicioSolicitado,
            @NotaInterna,
            @Estado,
            @FechaCreacionPendiente,
            @Version
        )
        """;

        var version =
            turno.Version <= 0
                ? 1
                : turno.Version;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", turno.Id.ToString());
        command.Parameters.AddWithValue("@TenantId", turno.TenantId.ToString());
        command.Parameters.AddWithValue("@FechaHora", turno.FechaHora);
        command.Parameters.AddWithValue("@NombreCliente", (object?)turno.NombreCliente ?? DBNull.Value);
        command.Parameters.AddWithValue("@TelefonoCliente", (object?)turno.TelefonoCliente ?? DBNull.Value);
        command.Parameters.AddWithValue("@ServicioSolicitado", (object?)turno.ServicioSolicitado ?? DBNull.Value);
        command.Parameters.AddWithValue("@NotaInterna", (object?)turno.NotaInterna ?? DBNull.Value);
        command.Parameters.AddWithValue("@Estado", (int)turno.Estado);
        command.Parameters.AddWithValue("@FechaCreacionPendiente", (object?)turno.FechaCreacionPendiente ?? DBNull.Value);
        command.Parameters.AddWithValue("@Version", version);

        await command.ExecuteNonQueryAsync();

        turno.Version = version;
    }

    public async Task ActualizarAsync(
        Turno turno)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        UPDATE Turnos
        SET
            NombreCliente = @NombreCliente,
            TelefonoCliente = @TelefonoCliente,
            ServicioSolicitado = @ServicioSolicitado,
            NotaInterna = @NotaInterna,
            Estado = @Estado,
            FechaCreacionPendiente = @FechaCreacionPendiente,
            Version = Version + 1
        WHERE Id = @Id
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", turno.Id.ToString());
        command.Parameters.AddWithValue("@NombreCliente", (object?)turno.NombreCliente ?? DBNull.Value);
        command.Parameters.AddWithValue("@TelefonoCliente", (object?)turno.TelefonoCliente ?? DBNull.Value);
        command.Parameters.AddWithValue("@ServicioSolicitado", (object?)turno.ServicioSolicitado ?? DBNull.Value);
        command.Parameters.AddWithValue("@NotaInterna", (object?)turno.NotaInterna ?? DBNull.Value);
        command.Parameters.AddWithValue("@Estado", (int)turno.Estado);
        command.Parameters.AddWithValue("@FechaCreacionPendiente", (object?)turno.FechaCreacionPendiente ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();

        turno.Version += 1;
    }

    private static Turno MapearTurno(
        System.Data.Common.DbDataReader reader)
    {
        return new Turno
        {
            Id = reader.GetGuid(0),
            TenantId = reader.GetGuid(1),
            FechaHora = reader.GetDateTime(2),

            NombreCliente =
                reader.IsDBNull(3)
                    ? null
                    : reader.GetString(3),

            TelefonoCliente =
                reader.IsDBNull(4)
                    ? null
                    : reader.GetString(4),

            ServicioSolicitado =
                reader.IsDBNull(5)
                    ? null
                    : reader.GetString(5),

            NotaInterna =
                reader.IsDBNull(6)
                    ? null
                    : reader.GetString(6),

            Estado =
                (TurnoEstado)
                reader.GetInt32(7),

            FechaCreacionPendiente =
                reader.IsDBNull(8)
                    ? null
                    : reader.GetDateTime(8),

            Version =
                reader.GetInt64(9)
        };
    }
}