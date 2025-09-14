using System.Data;
using Microsoft.Data.SqlClient;

namespace SqlServerHelper;

public static class SqlHelper
{
    private static string? _connectionString;

    /// <summary>
    /// Configura la cadena de conexión para SqlHelper. Debe llamarse una vez al inicio de la aplicación.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a SQL Server.</param>
    /// <example>
    /// SqlHelper.Configure("Server=myServer;Database=myDb;User Id=myUser;Password=myPass;");
    /// </example>
    public static void Configure(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Ejecuta un stored procedure que retorna una lista de objetos mapeados por <paramref name="mapFunction"/>.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto <typeparamref name="T"/>.</param>
    /// <returns>Lista de objetos mapeados.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var lista = await SqlHelper.ExecuteStoredProcedureListAsync("sp_GetMedicos", null, row => new Medico { Id = (int)row["Id"], Nombre = (string)row["Nombre"] });
    /// </example>
    public static async Task<List<T>> ExecuteStoredProcedureListAsync<T>(string storedProcedureName, Dictionary<string, object>? parameters = null, Func<DataRow, T>? mapFunction = null)
    {
        EnsureConfigured();

        var result = new List<T>();

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(storedProcedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        AddParameters(command, parameters);

        var dataTable = new DataTable();
        var adapter = new SqlDataAdapter(command);
        adapter.Fill(dataTable);

        foreach (DataRow row in dataTable.Rows)
        {
            result.Add(mapFunction!(row));
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// Agrega los parámetros al comando SQL.
    /// </summary>
    /// <param name="command">Comando SQL al que se agregan los parámetros.</param>
    /// <param name="parameters">Diccionario de parámetros (nombre, valor).</param>
    private static void AddParameters(SqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.Add(new SqlParameter(param.Key, param.Value ?? DBNull.Value));
            }
        }
    }
    
    /// <summary>
    /// Verifica que la cadena de conexión esté configurada antes de ejecutar cualquier método.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    private static void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("SqlHelper is not configured. Call SqlHelper.Configure(connectionString) on app startup.");
    }
}
