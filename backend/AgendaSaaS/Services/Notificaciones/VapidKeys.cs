using System.Security.Cryptography;
using AgendaSaaS.Data;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Services.Notificaciones;

/// <summary>
/// Par de claves VAPID (P-256) en base64url, como lo esperan el navegador
/// (applicationServerKey) y los servicios de push.
/// </summary>
public record VapidDetails(string Subject, string PublicKey, string PrivateKey)
{
    public static VapidDetails Generar(string subject)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parametros = ecdsa.ExportParameters(includePrivateParameters: true);

        // Clave pública sin comprimir: 0x04 || X || Y.
        var publica = new byte[65];
        publica[0] = 0x04;
        parametros.Q.X!.CopyTo(publica, 1);
        parametros.Q.Y!.CopyTo(publica, 33);

        return new VapidDetails(subject, Base64Url(publica), Base64Url(parametros.D!));
    }

    public static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// Claves VAPID con las que el servidor firma los push. Si no vienen por
/// configuración (WebPush:PublicKey / WebPush:PrivateKey) se generan una única vez y
/// se guardan en la base: si cambiaran, todas las suscripciones dejarían de valer.
/// </summary>
public class VapidKeys
{
    private const string ClavePublica = "WebPush:PublicKey";
    private const string ClavePrivada = "WebPush:PrivateKey";

    private readonly SqlConnectionFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private VapidDetails? _detalles;

    public VapidKeys(
        SqlConnectionFactory factory,
        IConfiguration configuration)
    {
        _factory = factory;
        _configuration = configuration;
    }

    public async Task<VapidDetails> ObtenerAsync()
    {
        if (_detalles != null)
            return _detalles;

        await _lock.WaitAsync();

        try
        {
            _detalles ??= await CargarAsync();
            return _detalles;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<VapidDetails> CargarAsync()
    {
        // Apple exige un "subject" mailto: o https: válido.
        var subject =
            _configuration["WebPush:Subject"]
            ?? $"mailto:{_configuration["SuperAdmin:Email"]}";

        var publica = _configuration[ClavePublica];
        var privada = _configuration[ClavePrivada];

        if (!string.IsNullOrWhiteSpace(publica) && !string.IsNullOrWhiteSpace(privada))
            return new VapidDetails(subject, publica, privada);

        using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        var generadas = VapidDetails.Generar(subject);

        // INSERT IGNORE: si dos instancias arrancan a la vez, gana la primera y
        // ambas terminan leyendo el mismo par.
        foreach (var (clave, valor) in new[]
                 {
                     (ClavePublica, generadas.PublicKey),
                     (ClavePrivada, generadas.PrivateKey)
                 })
        {
            using var insert = new MySqlCommand(
                "INSERT IGNORE INTO ConfiguracionSistema (Clave, Valor) VALUES (@Clave, @Valor)",
                connection);
            insert.Parameters.AddWithValue("@Clave", clave);
            insert.Parameters.AddWithValue("@Valor", valor);
            await insert.ExecuteNonQueryAsync();
        }

        return new VapidDetails(
            subject,
            await LeerAsync(connection, ClavePublica),
            await LeerAsync(connection, ClavePrivada));
    }

    private static async Task<string> LeerAsync(MySqlConnection connection, string clave)
    {
        using var select = new MySqlCommand(
            "SELECT Valor FROM ConfiguracionSistema WHERE Clave = @Clave",
            connection);
        select.Parameters.AddWithValue("@Clave", clave);
        return (string)(await select.ExecuteScalarAsync())!;
    }
}
