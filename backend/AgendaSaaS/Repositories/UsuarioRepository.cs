using AgendaSaaS.Data;
using AgendaSaaS.Entities;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly SqlConnectionFactory _factory;

    public UsuarioRepository(
        SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<Usuario?> ObtenerPorEmailAsync(
        string email)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
        SELECT
            Id,
            TenantId,
            Email,
            PasswordHash,
            Activo,
            Rol,
            TokenVersion
        FROM Usuarios
        WHERE Email = @Email
        LIMIT 1
        """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Email",
            email);

        using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new Usuario
        {
            Id = reader.GetGuid(0),

            TenantId = reader.GetGuid(1),

            Email =
                reader.GetString(2),

            PasswordHash =
                reader.GetString(3),

            Activo =
                reader.GetBoolean(4),

            Rol =
                (UsuarioRol)reader.GetInt32(5),

            TokenVersion =
                reader.GetInt32(6)
        };

    }

    public async Task<int?> ObtenerTokenVersionSiActivoAsync(
        Guid id)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        using var command =
            new MySqlCommand(
                "SELECT TokenVersion FROM Usuarios WHERE Id = @Id AND Activo = 1 LIMIT 1",
                connection);

        command.Parameters.AddWithValue(
            "@Id",
            id.ToString());

        var version =
            await command.ExecuteScalarAsync();

        return version is null or DBNull
            ? null
            : Convert.ToInt32(version);
    }

    public async Task CrearAsync(
    Usuario usuario)
    {
        using var connection =
            _factory.CreateConnection();

        await connection.OpenAsync();

        var sql =
        """
    INSERT INTO Usuarios
    (
        Id,
        TenantId,
        Email,
        PasswordHash,
        Activo,
        Rol
    )
    VALUES
    (
        @Id,
        @TenantId,
        @Email,
        @PasswordHash,
        @Activo,
        @Rol
    )
    """;

        using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Id",
            usuario.Id.ToString());

        command.Parameters.AddWithValue(
            "@TenantId",
            usuario.TenantId.ToString());

        command.Parameters.AddWithValue(
            "@Email",
            usuario.Email);

        command.Parameters.AddWithValue(
            "@PasswordHash",
            usuario.PasswordHash);

        command.Parameters.AddWithValue(
            "@Activo",
            usuario.Activo);

        command.Parameters.AddWithValue(
            "@Rol",
            (int)usuario.Rol);

        await command.ExecuteNonQueryAsync();
    }
}