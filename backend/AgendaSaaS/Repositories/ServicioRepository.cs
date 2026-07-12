using AgendaSaaS.Data;
using AgendaSaaS.Entities;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Repositories;

public class ServicioRepository : IServicioRepository
{
    private readonly SqlConnectionFactory _factory;

    public ServicioRepository(SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Servicio>> ObtenerPorTenantAsync(Guid tenantId)
    {
        var result = new List<Servicio>();

        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = """
            SELECT Id, TenantId, Nombre, Precio, Activo
            FROM servicios
            WHERE TenantId = @TenantId
            ORDER BY Nombre
            """;

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TenantId", tenantId.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(Map(reader));

        return result;
    }

    public async Task<IEnumerable<Servicio>> ObtenerActivosPorTenantAsync(Guid tenantId)
    {
        var result = new List<Servicio>();

        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = """
            SELECT Id, TenantId, Nombre, Precio, Activo
            FROM servicios
            WHERE TenantId = @TenantId AND Activo = 1
            ORDER BY Nombre
            """;

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TenantId", tenantId.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(Map(reader));

        return result;
    }

    public async Task<Servicio> CrearAsync(Servicio servicio)
    {
        servicio.Id = Guid.NewGuid();

        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = """
            INSERT INTO servicios (Id, TenantId, Nombre, Precio, Activo)
            VALUES (@Id, @TenantId, @Nombre, @Precio, @Activo)
            """;

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", servicio.Id.ToString());
        cmd.Parameters.AddWithValue("@TenantId", servicio.TenantId.ToString());
        cmd.Parameters.AddWithValue("@Nombre", servicio.Nombre);
        cmd.Parameters.AddWithValue("@Precio", servicio.Precio);
        cmd.Parameters.AddWithValue("@Activo", servicio.Activo ? 1 : 0);

        await cmd.ExecuteNonQueryAsync();

        return servicio;
    }

    public async Task<Servicio?> ActualizarAsync(Servicio servicio)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = """
            UPDATE servicios
            SET Nombre = @Nombre, Precio = @Precio, Activo = @Activo
            WHERE Id = @Id AND TenantId = @TenantId
            """;

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", servicio.Id.ToString());
        cmd.Parameters.AddWithValue("@TenantId", servicio.TenantId.ToString());
        cmd.Parameters.AddWithValue("@Nombre", servicio.Nombre);
        cmd.Parameters.AddWithValue("@Precio", servicio.Precio);
        cmd.Parameters.AddWithValue("@Activo", servicio.Activo ? 1 : 0);

        var rows = await cmd.ExecuteNonQueryAsync();

        return rows > 0 ? servicio : null;
    }

    public async Task<bool> EliminarAsync(Guid id, Guid tenantId)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var sql = "DELETE FROM servicios WHERE Id = @Id AND TenantId = @TenantId";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        cmd.Parameters.AddWithValue("@TenantId", tenantId.ToString());

        var rows = await cmd.ExecuteNonQueryAsync();

        return rows > 0;
    }

    private static Servicio Map(System.Data.Common.DbDataReader r) => new()
    {
        Id = r.GetGuid(0),
        TenantId = r.GetGuid(1),
        Nombre = r.GetString(2),
        Precio = r.GetDecimal(3),
        Activo = r.GetBoolean(4),
    };
}
