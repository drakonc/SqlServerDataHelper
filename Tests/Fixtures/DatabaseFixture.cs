using SqlHelper;

namespace Tests.Fixtures
{
    public class DatabaseFixture : IDisposable
    {
        public DatabaseFixture()
        {
            // Configura la cadena de conexión para la base de datos de pruebas
            SqlServerHelper.Configure("Server=localhost;Database=SqlHelperTestDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;");
        }

        public void Dispose()
        {
            // Limpieza de recursos
        }
    }
}
