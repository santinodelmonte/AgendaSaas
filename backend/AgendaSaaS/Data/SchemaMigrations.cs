using MySql.Data.MySqlClient;

namespace AgendaSaaS.Data;

public static class SchemaMigrations
{
    // Esquema base. Los nombres de tabla respetan mayúsculas: en MySQL sobre Linux
    // (lower_case_table_names=0) "Servicios" y "servicios" son tablas distintas.
    private static readonly string[] CrearTablas =
    [
        """
        CREATE TABLE IF NOT EXISTS Manicuristas (
            Id CHAR(36) NOT NULL,
            TenantId CHAR(36) NOT NULL,
            Nombre VARCHAR(200) NOT NULL,
            Slug VARCHAR(200) NOT NULL,
            Email VARCHAR(255) NOT NULL,
            WhatsApp VARCHAR(50) NOT NULL,
            Activo TINYINT(1) NOT NULL DEFAULT 1,
            DuracionTurnoMinutos INT NOT NULL DEFAULT 30,
            PRIMARY KEY (Id),
            UNIQUE KEY UX_Manicuristas_TenantId (TenantId),
            UNIQUE KEY UX_Manicuristas_Slug (Slug)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS Usuarios (
            Id CHAR(36) NOT NULL,
            TenantId CHAR(36) NOT NULL,
            Email VARCHAR(255) NOT NULL,
            PasswordHash VARCHAR(255) NOT NULL,
            Activo TINYINT(1) NOT NULL DEFAULT 1,
            Rol INT NOT NULL DEFAULT 0,
            PRIMARY KEY (Id),
            UNIQUE KEY UX_Usuarios_Email (Email),
            KEY IX_Usuarios_TenantId (TenantId)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS BloqueHorarioConfig (
            Id CHAR(36) NOT NULL,
            TenantId CHAR(36) NOT NULL,
            DiaSemana INT NOT NULL,
            HoraInicio TIME NOT NULL,
            HoraFin TIME NOT NULL,
            PausaInicio TIME NULL,
            PausaFin TIME NULL,
            PRIMARY KEY (Id),
            KEY IX_BloqueHorarioConfig_TenantId (TenantId)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS Turnos (
            Id CHAR(36) NOT NULL,
            TenantId CHAR(36) NOT NULL,
            FechaHora DATETIME NOT NULL,
            NombreCliente VARCHAR(100) NULL,
            TelefonoCliente VARCHAR(20) NULL,
            ServicioSolicitado VARCHAR(200) NULL,
            NotaInterna VARCHAR(500) NULL,
            Estado INT NOT NULL DEFAULT 0,
            FechaCreacionPendiente DATETIME NULL,
            Version BIGINT NOT NULL DEFAULT 1,
            DuracionMinutos INT NULL,
            PRIMARY KEY (Id),
            KEY IX_Turnos_TenantId_FechaHora (TenantId, FechaHora),
            KEY IX_Turnos_Estado_FechaCreacionPendiente (Estado, FechaCreacionPendiente)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS DiasBloqueados (
            Id CHAR(36) NOT NULL,
            TenantId CHAR(36) NOT NULL,
            Fecha DATE NOT NULL,
            Motivo VARCHAR(200) NULL,
            PRIMARY KEY (Id),
            UNIQUE KEY UX_DiasBloqueados_TenantId_Fecha (TenantId, Fecha)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,
        """
        CREATE TABLE IF NOT EXISTS Servicios (
            Id CHAR(36) NOT NULL,
            TenantId CHAR(36) NOT NULL,
            Nombre VARCHAR(200) NOT NULL,
            Precio DECIMAL(10,2) NOT NULL DEFAULT 0,
            Activo TINYINT(1) NOT NULL DEFAULT 1,
            PRIMARY KEY (Id),
            KEY IX_Servicios_TenantId (TenantId)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """
    ];

    /// <summary>
    /// El proyecto no usa un sistema de migraciones: el esquema se aplica al arrancar
    /// de forma idempotente. Crea las tablas que falten (una base vacía queda lista
    /// para usar) y aplica los cambios incrementales sobre bases existentes.
    /// </summary>
    public static async Task AplicarAsync(
        SqlConnectionFactory factory)
    {
        using var connection =
            factory.CreateConnection();

        await connection.OpenAsync();

        foreach (var sql in CrearTablas)
        {
            using var create =
                new MySqlCommand(sql, connection);

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
