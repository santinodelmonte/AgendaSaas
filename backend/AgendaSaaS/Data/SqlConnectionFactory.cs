using MySql.Data.MySqlClient;

namespace AgendaSaaS.Data;

public class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string no configurada.");
    }

    public MySqlConnection CreateConnection()
    {
        return new MySqlConnection(
            _connectionString);
    }
}