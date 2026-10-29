namespace AgendaSaaS.Tests.Infra;

/// <summary>
/// Test de integración contra MySQL real. Se saltea si no está definida la variable
/// AGENDASAAS_TEST_MYSQL (connection string al servidor, sin base de datos),
/// por ejemplo: "Server=localhost;Uid=root;Pwd=root;".
/// </summary>
public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(ApiFactory.ServidorMySql))
            Skip = "AGENDASAAS_TEST_MYSQL no definida: se omiten los tests de integración.";
    }
}
