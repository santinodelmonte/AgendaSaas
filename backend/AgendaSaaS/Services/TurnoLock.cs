using AgendaSaaS.Data;
using MySql.Data.MySqlClient;

namespace AgendaSaaS.Services;

/// <summary>
/// Lock por tenant con GET_LOCK de MySQL. Vive en una conexión dedicada que se
/// mantiene abierta mientras dura la operación; el resto de las queries usan sus
/// propias conexiones y otro request del mismo tenant espera en GET_LOCK.
/// </summary>
public class TurnoLock : ITurnoLock
{
    private const int TimeoutSegundos = 10;

    private readonly SqlConnectionFactory _factory;

    public TurnoLock(
        SqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IAsyncDisposable> AdquirirAsync(
        Guid tenantId)
    {
        var nombre = $"turnos:{tenantId}";

        var connection =
            _factory.CreateConnection();

        try
        {
            await connection.OpenAsync();

            using var command =
                new MySqlCommand(
                    "SELECT GET_LOCK(@Nombre, @Timeout)",
                    connection);

            command.Parameters.AddWithValue("@Nombre", nombre);
            command.Parameters.AddWithValue("@Timeout", TimeoutSegundos);

            var resultado =
                await command.ExecuteScalarAsync();

            if (resultado is DBNull or null || Convert.ToInt32(resultado) != 1)
                throw new LockNoDisponibleException();

            return new Liberador(connection, nombre);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class Liberador : IAsyncDisposable
    {
        private readonly MySqlConnection _connection;
        private readonly string _nombre;

        public Liberador(
            MySqlConnection connection,
            string nombre)
        {
            _connection = connection;
            _nombre = nombre;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                using var command =
                    new MySqlCommand(
                        "SELECT RELEASE_LOCK(@Nombre)",
                        _connection);

                command.Parameters.AddWithValue("@Nombre", _nombre);

                await command.ExecuteScalarAsync();
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}


/// <summary>
/// Otra operación sobre la agenda del mismo tenant tardó más que el timeout.
/// El manejador global de errores la traduce a un 409.
/// </summary>
public class LockNoDisponibleException : Exception
{
    public LockNoDisponibleException()
        : base("La agenda está ocupada procesando otro cambio. Probá de nuevo en unos segundos.")
    {
    }
}
