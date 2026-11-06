using MySql.Data.MySqlClient;

namespace AgendaSaaS.Data;

public static class SchemaMigrations
{
    /// <summary>
    /// El proyecto no usa un sistema de migraciones. Al arrancar se crean las tablas
    /// que falten (base nueva, ej. el primer deploy) y se aplican los cambios
    /// incrementales sobre bases existentes. Todo es idempotente.
    /// </summary>
    public static async Task AplicarAsync(
        SqlConnectionFactory factory)
    {
        using var connection =
            factory.CreateConnection();

        await connection.OpenAsync();

        foreach (var ddl in TablasBase)
        {
            using var create =
                new MySqlCommand(ddl, connection);

            await create.ExecuteNonQueryAsync();
        }

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

        // Se incrementa al resetear la contraseña: los JWT emitidos con la versión
        // anterior dejan de ser válidos.
        await AgregarColumnaSiFaltaAsync(
            connection, "Usuarios", "TokenVersion",
            "ALTER TABLE Usuarios ADD COLUMN TokenVersion INT NOT NULL DEFAULT 0");
    }

    // Esquema completo actual. Los nombres de tabla respetan mayúsculas porque en
    // MySQL sobre Linux son case-sensitive.
    private static readonly string[] TablasBase =
    [
        """
        CREATE TABLE IF NOT EXISTS Manicuristas (
            Id CHAR(36) NOT NULL PRIMARY KEY,
            TenantId CHAR(36) NOT NULL,
            Nombre VARCHAR(100) NOT NULL,
            Slug VARCHAR(100) NOT NULL,
            Email VARCHAR(200) NOT NULL,
            WhatsApp VARCHAR(30) NOT NULL,
            Activo TINYINT(1) NOT NULL DEFAULT 1,
            DuracionTurnoMinutos INT NOT NULL,
            UNIQUE KEY UX_Manicuristas_TenantId (TenantId),
            UNIQUE KEY UX_Manicuristas_Slug (Slug)
        ) DEFAULT CHARSET = utf8mb4
        """,
        """
        CREATE TABLE IF NOT EXISTS Usuarios (
            Id CHAR(36) NOT NULL PRIMARY KEY,
            TenantId CHAR(36) NOT NULL,
            Email VARCHAR(200) NOT NULL,
            PasswordHash VARCHAR(200) NOT NULL,
            Activo TINYINT(1) NOT NULL DEFAULT 1,
            Rol INT NOT NULL DEFAULT 0,
            TokenVersion INT NOT NULL DEFAULT 0,
            UNIQUE KEY UX_Usuarios_Email (Email)
        ) DEFAULT CHARSET = utf8mb4
        """,
        """
        CREATE TABLE IF NOT EXISTS Turnos (
            Id CHAR(36) NOT NULL PRIMARY KEY,
            TenantId CHAR(36) NOT NULL,
            FechaHora DATETIME NOT NULL,
            NombreCliente VARCHAR(100) NULL,
            TelefonoCliente VARCHAR(20) NULL,
            ServicioSolicitado VARCHAR(200) NULL,
            NotaInterna TEXT NULL,
            Estado INT NOT NULL,
            FechaCreacionPendiente DATETIME NULL,
            Version INT NOT NULL DEFAULT 1,
            DuracionMinutos INT NULL,
            KEY IX_Turnos_Tenant_FechaHora (TenantId, FechaHora)
        ) DEFAULT CHARSET = utf8mb4
        """,
        """
        CREATE TABLE IF NOT EXISTS BloqueHorarioConfig (
            Id CHAR(36) NOT NULL PRIMARY KEY,
            TenantId CHAR(36) NOT NULL,
            DiaSemana INT NOT NULL,
            HoraInicio TIME NOT NULL,
            HoraFin TIME NOT NULL,
            PausaInicio TIME NULL,
            PausaFin TIME NULL,
            KEY IX_BloqueHorarioConfig_TenantId (TenantId)
        ) DEFAULT CHARSET = utf8mb4
        """,
        """
        CREATE TABLE IF NOT EXISTS DiasBloqueados (
            Id CHAR(36) NOT NULL PRIMARY KEY,
            TenantId CHAR(36) NOT NULL,
            Fecha DATE NOT NULL,
            Motivo VARCHAR(200) NULL,
            KEY IX_DiasBloqueados_Tenant_Fecha (TenantId, Fecha)
        ) DEFAULT CHARSET = utf8mb4
        """,
        """
        CREATE TABLE IF NOT EXISTS Servicios (
            Id CHAR(36) NOT NULL PRIMARY KEY,
            TenantId CHAR(36) NOT NULL,
            Nombre VARCHAR(100) NOT NULL,
            Precio DECIMAL(10,2) NOT NULL,
            Activo TINYINT(1) NOT NULL DEFAULT 1,
            KEY IX_Servicios_TenantId (TenantId)
        ) DEFAULT CHARSET = utf8mb4
        """
    ];

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
