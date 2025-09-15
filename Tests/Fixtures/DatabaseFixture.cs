using Microsoft.Data.SqlClient;
using SqlHelper;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Tests.Fixtures
{
    public class DatabaseFixture : IDisposable
    {
        public DatabaseFixture()
        {
            // Leer la cadena de conexión desde appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var testDbConnectionString = config.GetConnectionString("TestDbConnection");

            // 1. Crear base de datos temporal
            using var masterConn = new SqlConnection(testDbConnectionString);
            masterConn.Open();
            using (var cmd = new SqlCommand("IF DB_ID('SqlHelperTestDb') IS NOT NULL DROP DATABASE SqlHelperTestDb; CREATE DATABASE SqlHelperTestDb;", masterConn))
            {
                cmd.ExecuteNonQuery();
            }

            // 2. Ejecutar script de estructura y datos desde archivo relativo
            var scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Tests", "TestData", "example-database.sql");
            var sql = File.ReadAllText(scriptPath);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                using var testConn = new SqlConnection(testDbConnectionString);
                testConn.Open();
                foreach (var batch in sql.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    using var cmd = new SqlCommand(batch, testConn);
                    cmd.ExecuteNonQuery();
                }
            }

            // 3. Configurar helper para usar la BD de pruebas
            if (string.IsNullOrWhiteSpace(testDbConnectionString))
                throw new InvalidOperationException("La cadena de conexión 'TestDbConnection' no está configurada en appsettings.json.");
            SqlServerHelper.Configure(testDbConnectionString);
        }

        public void Dispose()
        {
            // Leer la cadena de conexión desde appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var testDbConnectionString = config.GetConnectionString("TestDbConnection");

            // Eliminar la base de datos temporal al finalizar las pruebas
            using var masterConn = new SqlConnection(testDbConnectionString);
            masterConn.Open();
            using var cmd = new SqlCommand("IF DB_ID('SqlHelperTestDb') IS NOT NULL DROP DATABASE SqlHelperTestDb;", masterConn);
            cmd.ExecuteNonQuery();
        }
    }
}
