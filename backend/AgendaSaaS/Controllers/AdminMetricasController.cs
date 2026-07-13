using AgendaSaaS.Data;
using AgendaSaaS.Services;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Controllers;

[ApiController]
[Route("api/admin/metricas")]
public class AdminMetricasController : ControllerBase
{
    private readonly ITenantProvider _tenantProvider;
    private readonly SqlConnectionFactory _factory;

    public AdminMetricasController(
        ITenantProvider tenantProvider,
        SqlConnectionFactory factory)
    {
        _tenantProvider = tenantProvider;
        _factory = factory;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var tenantId = _tenantProvider.TenantId.ToString();

        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        // Turnos por mes (últimos 6 meses)
        var porMes = new List<object>();
        var sqlMes =
        """
        SELECT
            DATE_FORMAT(FechaHora, '%Y-%m') AS Mes,
            COUNT(*) AS Total,
            SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS Confirmados
        FROM Turnos
        WHERE TenantId = @TenantId
          AND Estado != 0
          AND FechaHora >= DATE_SUB(NOW(), INTERVAL 6 MONTH)
        GROUP BY DATE_FORMAT(FechaHora, '%Y-%m')
        ORDER BY Mes
        """;

        using (var cmd = new MySqlCommand(sqlMes, connection))
        {
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                porMes.Add(new
                {
                    Mes = reader.GetString(0),
                    Total = reader.GetInt32(1),
                    Confirmados = reader.GetInt32(2)
                });
            }
        }

        // Tasa de confirmación
        int totalPasados = 0, confirmados = 0, rechazados = 0;
        var sqlTasa =
        """
        SELECT
            COUNT(*) AS Total,
            SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS Confirmados,
            SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END) AS Rechazados
        FROM Turnos
        WHERE TenantId = @TenantId
          AND Estado != 0
          AND FechaHora < NOW()
        """;

        using (var cmd = new MySqlCommand(sqlTasa, connection))
        {
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                totalPasados = reader.GetInt32(0);
                confirmados = reader.GetInt32(1);
                rechazados = reader.GetInt32(2);
            }
        }

        // Horas más pedidas (top 5)
        var horasMasPedidas = new List<object>();
        var sqlHoras =
        """
        SELECT
            HOUR(FechaHora) AS Hora,
            COUNT(*) AS Total
        FROM Turnos
        WHERE TenantId = @TenantId
          AND Estado != 0
        GROUP BY HOUR(FechaHora)
        ORDER BY Total DESC
        LIMIT 5
        """;

        using (var cmd = new MySqlCommand(sqlHoras, connection))
        {
            cmd.Parameters.AddWithValue("@TenantId", tenantId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                horasMasPedidas.Add(new
                {
                    Hora = reader.GetInt32(0),
                    Total = reader.GetInt32(1)
                });
            }
        }

        double tasaConfirmacion = totalPasados > 0
            ? Math.Round((double)confirmados / totalPasados * 100, 1)
            : 0;

        return Ok(new
        {
            PorMes = porMes,
            TasaConfirmacion = tasaConfirmacion,
            TotalPasados = totalPasados,
            Confirmados = confirmados,
            Rechazados = rechazados,
            HorasMasPedidas = horasMasPedidas
        });
    }
}
