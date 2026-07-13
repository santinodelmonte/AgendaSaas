using MySql.Data.MySqlClient;

namespace AgendaSaaS.Data;

public static class SchemaMigrations
{
    /// <summary>
    /// El proyecto no usa un sistema de migraciones: el esquema se mantiene a mano.
    /// Este chequeo idempotente agrega la columna Turnos.DuracionMinutos si falta,
    /// para que la app no dependa de correr el ALTER manualmente.
    /// </summary>
    public static async Task AplicarAsync(
        SqlConnectionFactory factory)
    {
        using var connection =
            factory.CreateConnection();

        await connection.OpenAsync();

        await AgregarColumnaSiFaltaAsync(
            connection, "Turnos", "DuracionMinutos",
            "ALTER TABLE Turnos ADD COLUMN DuracionMinutos INT NULL");

        await AgregarColumnaSiFaltaAsync(
            connection, "Usuarios", "Rol",
            "ALTER TABLE Usuarios ADD COLUMN Rol INT NOT NULL DEFAULT 0");

        // El superadmin es un usuario sin manicurista asociada, así que la FK
        // Usuarios.TenantId -> Manicuristas.TenantId ya no puede ser obligatoria.
        // La integridad de los usuarios de manicuristas la garantiza la creación
        // transaccional (Manicurista + Usuario juntos).
        await EliminarForeignKeySiExisteAsync(
            connection, "Usuarios", "FK_Usuarios_Manicuristas");
    }

    private static async Task EliminarForeignKeySiExisteAsync(
        MySqlConnection connection,
        string tabla,
        string constraint)
    {
        var sqlExiste =
        """
        SELECT COUNT(*)
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = @Tabla
          AND CONSTRAINT_NAME = @Constraint
          AND CONSTRAINT_TYPE = 'FOREIGN KEY'
        """;

        using var check =
            new MySqlCommand(sqlExiste, connection);

        check.Parameters.AddWithValue("@Tabla", tabla);
        check.Parameters.AddWithValue("@Constraint", constraint);

        var existe =
            Convert.ToInt32(
                await check.ExecuteScalarAsync()) > 0;

        if (!existe)
            return;

        using var drop =
            new MySqlCommand(
                $"ALTER TABLE {tabla} DROP FOREIGN KEY {constraint}",
                connection);

        await drop.ExecuteNonQueryAsync();
    }

    private static async Task AgregarColumnaSiFaltaAsync(
        MySqlConnection connection,
        string tabla,
        string columna,
        string alterSql)
    {
        var sqlExiste =
        """
        SELECT COUNT(*)
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = @Tabla
          AND COLUMN_NAME = @Columna
        """;

        using var check =
            new MySqlCommand(sqlExiste, connection);

        check.Parameters.AddWithValue("@Tabla", tabla);
        check.Parameters.AddWithValue("@Columna", columna);

        var existe =
            Convert.ToInt32(
                await check.ExecuteScalarAsync()) > 0;

        if (existe)
            return;

        using var alter =
            new MySqlCommand(alterSql, connection);

        await alter.ExecuteNonQueryAsync();
    }
}
