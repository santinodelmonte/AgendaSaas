using AgendaSaaS.Data;
using AgendaSaaS.DTOs;
using AgendaSaaS.Entities;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Repositories;

public class SuperAdminRepository : ISuperAdminRepository
{
    private readonly SqlConnectionFactory _factory;

    public SuperAdminRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<ManicuristaAdminDto>> ListarAsync()
    {
        var resultado = new List<ManicuristaAdminDto>();

        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var sql =
        """
        SELECT
            m.TenantId,
            m.Nombre,
            m.Slug,
            m.Email,
            m.WhatsApp,
            m.Activo,
            m.DuracionTurnoMinutos,
            u.Email AS LoginEmail,
            u.Activo AS UsuarioActivo,
            (SELECT COUNT(*) FROM Turnos t WHERE t.TenantId = m.TenantId) AS CantidadTurnos
        FROM Manicuristas m
        LEFT JOIN Usuarios u ON u.TenantId = m.TenantId
        ORDER BY m.Nombre
        """;

        using var command = new MySqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(new ManicuristaAdminDto
            {
                TenantId = reader.GetGuid(0),
                Nombre = reader.GetString(1),
                Slug = reader.GetString(2),
                Email = reader.GetString(3),
                WhatsApp = reader.GetString(4),
                Activo = reader.GetBoolean(5),
                DuracionTurnoMinutos = reader.GetInt32(6),
                LoginEmail = reader.IsDBNull(7) ? null : reader.GetString(7),
                UsuarioActivo = !reader.IsDBNull(8) && reader.GetBoolean(8),
                CantidadTurnos = reader.GetInt32(9)
            });
        }

        return resultado;
    }

    public async Task CrearAsync(
        Manicurista manicurista,
        Usuario usuario,
        List<BloqueHorarioConfig> horarios)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        using var tx = connection.BeginTransaction();

        try
        {
            var mSql =
            """
            INSERT INTO Manicuristas
                (Id, TenantId, Nombre, Slug, Email, WhatsApp, Activo, DuracionTurnoMinutos)
            VALUES
                (@Id, @TenantId, @Nombre, @Slug, @Email, @WhatsApp, @Activo, @Dur)
            """;

            using (var cmd = new MySqlCommand(mSql, connection, tx))
            {
                cmd.Parameters.AddWithValue("@Id", manicurista.Id.ToString());
                cmd.Parameters.AddWithValue("@TenantId", manicurista.TenantId.ToString());
                cmd.Parameters.AddWithValue("@Nombre", manicurista.Nombre);
                cmd.Parameters.AddWithValue("@Slug", manicurista.Slug);
                cmd.Parameters.AddWithValue("@Email", manicurista.Email);
                cmd.Parameters.AddWithValue("@WhatsApp", manicurista.WhatsApp);
                cmd.Parameters.AddWithValue("@Activo", manicurista.Activo);
                cmd.Parameters.AddWithValue("@Dur", manicurista.DuracionTurnoMinutos);
                await cmd.ExecuteNonQueryAsync();
            }

            var uSql =
            """
            INSERT INTO Usuarios
                (Id, TenantId, Email, PasswordHash, Activo, Rol)
            VALUES
                (@Id, @TenantId, @Email, @PasswordHash, @Activo, @Rol)
            """;

            using (var cmd = new MySqlCommand(uSql, connection, tx))
            {
                cmd.Parameters.AddWithValue("@Id", usuario.Id.ToString());
                cmd.Parameters.AddWithValue("@TenantId", usuario.TenantId.ToString());
                cmd.Parameters.AddWithValue("@Email", usuario.Email);
                cmd.Parameters.AddWithValue("@PasswordHash", usuario.PasswordHash);
                cmd.Parameters.AddWithValue("@Activo", usuario.Activo);
                cmd.Parameters.AddWithValue("@Rol", (int)usuario.Rol);
                await cmd.ExecuteNonQueryAsync();
            }

            var hSql =
            """
            INSERT INTO BloqueHorarioConfig
                (Id, TenantId, DiaSemana, HoraInicio, HoraFin, PausaInicio, PausaFin)
            VALUES
                (@Id, @TenantId, @DiaSemana, @HoraInicio, @HoraFin, @PausaInicio, @PausaFin)
            """;

            foreach (var h in horarios)
            {
                using var cmd = new MySqlCommand(hSql, connection, tx);
                cmd.Parameters.AddWithValue("@Id", h.Id.ToString());
                cmd.Parameters.AddWithValue("@TenantId", h.TenantId.ToString());
                cmd.Parameters.AddWithValue("@DiaSemana", (int)h.DiaSemana);
                cmd.Parameters.AddWithValue("@HoraInicio", h.HoraInicio);
                cmd.Parameters.AddWithValue("@HoraFin", h.HoraFin);
                cmd.Parameters.AddWithValue("@PausaInicio", (object?)h.PausaInicio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PausaFin", (object?)h.PausaFin ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task ActualizarAsync(
        Guid tenantId,
        string nombre,
        string slug,
        string whatsApp,
        int duracionTurnoMinutos)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var sql =
        """
        UPDATE Manicuristas
        SET Nombre = @Nombre,
            Slug = @Slug,
            WhatsApp = @WhatsApp,
            DuracionTurnoMinutos = @Dur
        WHERE TenantId = @TenantId
        """;

        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Nombre", nombre);
        cmd.Parameters.AddWithValue("@Slug", slug);
        cmd.Parameters.AddWithValue("@WhatsApp", whatsApp);
        cmd.Parameters.AddWithValue("@Dur", duracionTurnoMinutos);
        cmd.Parameters.AddWithValue("@TenantId", tenantId.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetActivoAsync(Guid tenantId, bool activo)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        using var tx = connection.BeginTransaction();
        try
        {
            using (var cmd = new MySqlCommand(
                "UPDATE Manicuristas SET Activo = @Activo WHERE TenantId = @TenantId",
                connection, tx))
            {
                cmd.Parameters.AddWithValue("@Activo", activo);
                cmd.Parameters.AddWithValue("@TenantId", tenantId.ToString());
                await cmd.ExecuteNonQueryAsync();
            }

            // Desactivar también impide el login de la manicurista.
            using (var cmd = new MySqlCommand(
                "UPDATE Usuarios SET Activo = @Activo WHERE TenantId = @TenantId",
                connection, tx))
            {
                cmd.Parameters.AddWithValue("@Activo", activo);
                cmd.Parameters.AddWithValue("@TenantId", tenantId.ToString());
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task ResetPasswordAsync(Guid tenantId, string passwordHash)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        using var cmd = new MySqlCommand(
            "UPDATE Usuarios SET PasswordHash = @Hash WHERE TenantId = @TenantId",
            connection);
        cmd.Parameters.AddWithValue("@Hash", passwordHash);
        cmd.Parameters.AddWithValue("@TenantId", tenantId.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EliminarAsync(Guid tenantId)
    {
        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        using var tx = connection.BeginTransaction();
        try
        {
            // Borrado en cascada: todo lo asociado al tenant.
            var tablas = new[]
            {
                "Turnos",
                "DiasBloqueados",
                "Servicios",
                "BloqueHorarioConfig",
                "Usuarios",
                "Manicuristas"
            };

            foreach (var tabla in tablas)
            {
                using var cmd = new MySqlCommand(
                    $"DELETE FROM {tabla} WHERE TenantId = @TenantId",
                    connection, tx);
                cmd.Parameters.AddWithValue("@TenantId", tenantId.ToString());
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
