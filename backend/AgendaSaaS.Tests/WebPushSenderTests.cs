using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgendaSaaS.Data;
using AgendaSaaS.Entities;
using AgendaSaaS.Services.Notificaciones;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgendaSaaS.Tests;

/// <summary>
/// Prueba el envío real contra un servicio de push local que hace lo mismo que el
/// navegador del teléfono: valida la firma VAPID y descifra el mensaje (RFC 8291,
/// aes128gcm, el formato que exige Apple).
/// </summary>
public sealed class WebPushSenderTests : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _baseUrl;

    public WebPushSenderTests()
    {
        _baseUrl = $"http://127.0.0.1:{PuertoLibre()}/";
        _listener.Prefixes.Add(_baseUrl);
        _listener.Start();
    }

    public void Dispose() => _listener.Close();

    [Fact]
    public async Task El_dispositivo_puede_verificar_y_descifrar_la_notificacion()
    {
        using var dispositivo = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var authSecret = RandomNumberGenerator.GetBytes(16);
        var vapid = VapidDetails.Generar("mailto:tests@tests.local");

        var recibido = AtenderUnaVezAsync(201);

        var resultado = await CrearSender(vapid).EnviarAsync(
            Suscripcion(dispositivo, authSecret),
            new NotificacionPush("Nueva reserva 💅", "Ana · lun 14/1 10:00", "/admin/turnos-pendientes", "t1"));

        var request = await recibido;
        Assert.Equal(ResultadoPush.Enviado, resultado);
        Assert.Equal("aes128gcm", request.ContentEncoding);
        Assert.Equal("86400", request.Ttl);
        Assert.Equal("high", request.Urgency);

        // Authorization: vapid t=<JWT ES256>, k=<clave pública del servidor>
        var auth = request.Authorization!;
        Assert.StartsWith("vapid ", auth);
        var partes = auth["vapid ".Length..].Split(',', StringSplitOptions.TrimEntries)
            .ToDictionary(p => p[..p.IndexOf('=')], p => p[(p.IndexOf('=') + 1)..]);
        Assert.Equal(vapid.PublicKey, partes["k"]);
        // El "aud" es el origen del servicio de push (sin puerto: los reales usan el 443).
        VerificarJwt(partes["t"], vapid.PublicKey, "http://127.0.0.1");

        var json = Descifrar(request.Body, dispositivo, authSecret);
        var notificacion = JsonDocument.Parse(json).RootElement;
        Assert.Equal("Nueva reserva 💅", notificacion.GetProperty("titulo").GetString());
        Assert.Equal("/admin/turnos-pendientes", notificacion.GetProperty("url").GetString());
    }

    [Theory]
    [InlineData(410, ResultadoPush.SuscripcionVencida)]
    [InlineData(404, ResultadoPush.SuscripcionVencida)]
    [InlineData(400, ResultadoPush.Error)]
    public async Task Traduce_las_respuestas_del_servicio_de_push(int status, ResultadoPush esperado)
    {
        using var dispositivo = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var recibido = AtenderUnaVezAsync(status);

        var resultado = await CrearSender(VapidDetails.Generar("mailto:tests@tests.local")).EnviarAsync(
            Suscripcion(dispositivo, RandomNumberGenerator.GetBytes(16)),
            new NotificacionPush("t", "c", "/admin", "t"));

        await recibido;
        Assert.Equal(esperado, resultado);
    }

    private record Recibido(string? ContentEncoding, string? Authorization, string? Ttl, string? Urgency, byte[] Body);

    private async Task<Recibido> AtenderUnaVezAsync(int status)
    {
        var context = await _listener.GetContextAsync();
        using var body = new MemoryStream();
        await context.Request.InputStream.CopyToAsync(body);

        var recibido = new Recibido(
            context.Request.Headers["Content-Encoding"],
            context.Request.Headers["Authorization"],
            context.Request.Headers["TTL"],
            context.Request.Headers["Urgency"],
            body.ToArray());

        context.Response.StatusCode = status;
        context.Response.Close();
        return recibido;
    }

    private PushSuscripcion Suscripcion(ECDiffieHellman dispositivo, byte[] authSecret) => new()
    {
        Id = Guid.NewGuid(),
        Endpoint = $"{_baseUrl}push/{Guid.NewGuid():N}",
        P256dh = VapidDetails.Base64Url(ClavePublica(dispositivo)),
        Auth = VapidDetails.Base64Url(authSecret)
    };

    // RFC 8291 desde el lado del navegador.
    private static string Descifrar(byte[] body, ECDiffieHellman dispositivo, byte[] authSecret)
    {
        var salt = body[..16];
        var largoKeyId = body[20];
        var clavePublicaServidor = body[21..(21 + largoKeyId)];
        var cifrado = body[(21 + largoKeyId)..];

        using var servidor = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = clavePublicaServidor[1..33], Y = clavePublicaServidor[33..65] }
        });
        var secreto = dispositivo.DeriveRawSecretAgreement(servidor.PublicKey);

        var keyInfo = Encoding.ASCII.GetBytes("WebPush: info\0")
            .Concat(ClavePublica(dispositivo)).Concat(clavePublicaServidor).ToArray();
        var ikm = HKDF.DeriveKey(HashAlgorithmName.SHA256, secreto, 32, authSecret, keyInfo);
        var cek = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 16, salt, Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0"));
        var nonce = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 12, salt, Encoding.ASCII.GetBytes("Content-Encoding: nonce\0"));

        var texto = new byte[cifrado.Length - 16];
        using var aes = new AesGcm(cek, 16);
        aes.Decrypt(nonce, cifrado[..^16], cifrado[^16..], texto);

        // El último registro termina con el delimitador 0x02 seguido de padding.
        var fin = Array.LastIndexOf(texto, (byte)0x02);
        return Encoding.UTF8.GetString(texto, 0, fin);
    }

    private static void VerificarJwt(string jwt, string clavePublica, string audienciaEsperada)
    {
        var partes = jwt.Split('.');
        var publica = DesdeBase64Url(clavePublica);

        using var ecdsa = ECDsa.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = publica[1..33], Y = publica[33..65] }
        });

        Assert.True(ecdsa.VerifyData(
            Encoding.ASCII.GetBytes($"{partes[0]}.{partes[1]}"),
            DesdeBase64Url(partes[2]),
            HashAlgorithmName.SHA256));

        var claims = JsonDocument.Parse(DesdeBase64Url(partes[1])).RootElement;
        Assert.Equal(audienciaEsperada, claims.GetProperty("aud").GetString());
        Assert.Equal("mailto:tests@tests.local", claims.GetProperty("sub").GetString());
        Assert.True(claims.GetProperty("exp").GetInt64() > DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private static WebPushSender CrearSender(VapidDetails vapid)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "no-se-usa",
                ["WebPush:PublicKey"] = vapid.PublicKey,
                ["WebPush:PrivateKey"] = vapid.PrivateKey,
                ["WebPush:Subject"] = vapid.Subject
            })
            .Build();

        return new WebPushSender(
            new VapidKeys(new SqlConnectionFactory(config), config),
            new SinProxyHttpClientFactory(),
            NullLogger<WebPushSender>.Instance);
    }

    private static byte[] ClavePublica(ECDiffieHellman ecdh)
    {
        var q = ecdh.ExportParameters(false).Q;
        return [0x04, .. q.X!, .. q.Y!];
    }

    private static byte[] DesdeBase64Url(string valor)
    {
        var b64 = valor.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '='));
    }

    private static int PuertoLibre()
    {
        var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }

    private sealed class SinProxyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new HttpClientHandler { UseProxy = false });
    }
}
