using System.Data;
using Microsoft.Data.SqlClient;
using SqlServerHelper.Models;

namespace SqlServerHelper;

public static class SqlServerHelper
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
        try
        {
            _connectionString = connectionString;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al configurar la cadena de conexión: {ex.Message}", ex);
        }
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
        try
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

            int rowIndex = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    result.Add(mapFunction!(row));
                }
                catch (Exception ex)
                {
                    // Error de mapeo, solo mostramos ese mensaje
                    throw new Exception($"Error al mapear la fila {rowIndex} del resultado en '{storedProcedureName}': {ex.Message}");
                }
                rowIndex++;
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            // Si el error es de mapeo, ya viene con el mensaje exacto
            if (ex.Message.StartsWith("Error al mapear"))
                throw;
            throw new Exception($"Error al ejecutar el stored procedure '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta un stored procedure y retorna el primer resultado mapeado por <paramref name="mapFunction"/> de forma asíncrona.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="mapFunction">Función para mapear la fila del resultado a un objeto <typeparamref name="T"/>.</param>
    /// <returns>Primer objeto mapeado o <c>default</c> si no hay resultados.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var medico = await SqlHelper.ExecuteStoredProcedureSingleAsync("sp_GetMedico", null, row => new Medico { Id = (int)row["Id"], Nombre = (string)row["Nombre"] });
    /// </example>
    public static async Task<T?> ExecuteStoredProcedureSingleAsync<T>(string storedProcedureName, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null)
    {
        try
        {
            EnsureConfigured();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(command, parameters);
            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                try
                {
                    return mapFunction!(reader);
                }
                catch (Exception ex)
                {
                    // Error de mapeo, solo mostramos ese mensaje
                    throw new Exception($"Error al mapear el resultado en '{storedProcedureName}': {ex.Message}");
                }
            }

            return default;
        }
        catch (Exception ex)
        {
            if (ex.Message.StartsWith("Error al mapear"))
                throw;
            throw new Exception($"Error al ejecutar el stored procedure '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta un stored procedure que no retorna resultados (por ejemplo, para operaciones de inserción, actualización o borrado).
    /// </summary>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <returns><c>true</c> si la ejecución fue exitosa.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// await SqlHelper.ExecuteStoredProcedureAsync("sp_InsertMedico", new Dictionary<string, object> { { "Nombre", "Juan" }, { "Especialidad", "Cardiología" } });
    /// </example>
    public static async Task<bool> ExecuteStoredProcedureAsync(string storedProcedureName, Dictionary<string, object>? parameters = null)
    {
        try
        {
            EnsureConfigured();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(command, parameters);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al ejecutar el stored procedure '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL que retorna una lista de objetos mapeados por <paramref name="mapFunction"/>.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto <typeparamref name="T"/>.</param>
    /// <returns>Lista de objetos mapeados.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var lista = await SqlHelper.ExecuteQueryListAsync("SELECT * FROM Medicos", null, row => new Medico { Id = (int)row["Id"], Nombre = (string)row["Nombre"] });
    /// </example>
    /// <summary>
    /// Ejecuta una consulta SQL de forma asíncrona y retorna una lista de objetos mapeados por <paramref name="mapFunction"/>.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto <typeparamref name="T"/>.</param>
    /// <returns>Lista de objetos mapeados.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var lista = await SqlHelper.ExecuteQueryListAsync("SELECT * FROM Medicos", null, reader => new Medico { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
    /// </example>
    public static async Task<List<T>> ExecuteQueryListAsync<T>(string sqlQuery, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null)
    {
        try
        {
            EnsureConfigured();

            var result = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection);

            AddParameters(command, parameters);
            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            int rowIndex = 0;
            while (await reader.ReadAsync())
            {
                try
                {
                    result.Add(mapFunction!(reader));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error al mapear la fila {rowIndex} del resultado en la consulta '{sqlQuery}': {ex.Message}");
                }
                rowIndex++;
            }

            return result;
        }
        catch (Exception ex)
        {
            if (ex.Message.StartsWith("Error al mapear"))
                throw;
            throw new Exception($"Error al ejecutar la consulta SQL '{sqlQuery}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL de forma asíncrona y retorna el primer resultado mapeado por <paramref name="mapFunction"/>.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <param name="mapFunction">Función para mapear la fila del resultado a un objeto <typeparamref name="T"/>.</param>
    /// <returns>Primer objeto mapeado o <c>default</c> si no hay resultados.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var medico = await SqlHelper.ExecuteQuerySingleAsync("SELECT * FROM Medicos WHERE Id = 1", null, reader => new Medico { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
    /// </example>
    public static async Task<T?> ExecuteQuerySingleAsync<T>(string sqlQuery, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null)
    {
        try
        {
            EnsureConfigured();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection);

            AddParameters(command, parameters);
            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                try
                {
                    return mapFunction!(reader);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error al mapear el resultado en la consulta '{sqlQuery}': {ex.Message}");
                }
            }

            return default;
        }
        catch (Exception ex)
        {
            if (ex.Message.StartsWith("Error al mapear"))
                throw;
            throw new Exception($"Error al ejecutar la consulta SQL '{sqlQuery}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL que no retorna resultados (por ejemplo, para operaciones de inserción, actualización o borrado) de forma asíncrona.
    /// </summary>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <returns>Número de filas afectadas por la consulta.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// await SqlHelper.ExecuteQueryAsync("UPDATE Medicos SET Nombre = 'Juan' WHERE Id = 1");
    /// </example>
    public static async Task<int> ExecuteQueryAsync(string sqlQuery, Dictionary<string, object>? parameters = null)
    {
        try
        {
            EnsureConfigured();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection);

            AddParameters(command, parameters);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al ejecutar la consulta SQL '{sqlQuery}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta un stored procedure que retorna múltiples tablas en un DataSet.
    /// </summary>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <returns>DataSet con las tablas retornadas.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var ds = await SqlHelper.ExecuteStoredProcedureMultipleTablesAsync("sp_GetTablas", null);
    /// </example>
    public static async Task<DataSet> ExecuteStoredProcedureMultipleTablesAsync(string storedProcedureName, Dictionary<string, object>? parameters = null)
    {
        try
        {
            EnsureConfigured();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcedureName, connection){ CommandType = CommandType.StoredProcedure };
            AddParameters(command, parameters);
            var dataSet = new DataSet();
            var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataSet);
            return await Task.FromResult(dataSet);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al ejecutar el stored procedure '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL que retorna múltiples tablas en un DataSet.
    /// </summary>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <returns>DataSet con las tablas retornadas.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var ds = await SqlHelper.ExecuteQueryMultipleTablesAsync("SELECT * FROM Tabla1; SELECT * FROM Tabla2", null);
    /// </example>
    public static async Task<DataSet> ExecuteQueryMultipleTablesAsync(string sqlQuery, Dictionary<string, object>? parameters = null)
    {
        try
        {
            EnsureConfigured();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection);
            AddParameters(command, parameters);
            var dataSet = new DataSet();
            var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataSet);
            return await Task.FromResult(dataSet);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al ejecutar la consulta SQL '{sqlQuery}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta un stored procedure paginado y retorna los datos junto con la información de paginación.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="pageNumber">Número de página.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto <typeparamref name="T"/>.</param>
    /// <returns>PagedResult con los datos y la información de paginación.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var paged = await SqlHelper.ExecuteStoredProcedurePaginatedAsync("sp_GetMedicosPaged", 1, 10, null, row => ...);
    /// </example>
    public static async Task<PagedResult<T>> ExecuteStoredProcedurePaginatedAsync<T>(string storedProcedureName, int pageNumber, int pageSize, Dictionary<string, object>? parameters = null, Func<DataRow, T>? mapFunction = null)
    {
        try
        {
            EnsureConfigured();
            var allParameters = new Dictionary<string, object>(parameters ?? new()) { { "@PageNumber", pageNumber },{ "@PageSize", pageSize } };

            var outputParams = new Dictionary<string, SqlDbType> { { "@TotalRecords", SqlDbType.Int } };

            var result = await ExecuteStoredProcedureWithOutputAsync(storedProcedureName, allParameters, outputParams, mapFunction);
            var totalRecords = Convert.ToInt32(result.OutputParameters["@TotalRecords"]);
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            return new PagedResult<T>
            {
                Data = result.Data,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };
        }
        catch (Exception ex)
        {
            if (ex.Message.StartsWith("Error al mapear"))
                throw;
            throw new Exception($"Error al ejecutar el stored procedure paginado '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta paginada y retorna los datos junto con la información de paginación.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="baseQuery">Consulta base para obtener los datos.</param>
    /// <param name="countQuery">Consulta para obtener el total de registros.</param>
    /// <param name="pageNumber">Número de página.</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto <typeparamref name="T"/>.</param>
    /// <returns>PagedResult con los datos y la información de paginación.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var paged = await SqlHelper.ExecuteQueryPaginatedAsync("SELECT * FROM Medicos", "SELECT COUNT(*) FROM Medicos", 1, 10, null, reader => ...);
    /// </example>
    public static async Task<PagedResult<T>> ExecuteQueryPaginatedAsync<T>(string baseQuery, string countQuery, int pageNumber, int pageSize, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null)
    {
        EnsureConfigured();

        var totalRecords = await ExecuteQuerySingleAsync<int>(
            countQuery, parameters, reader => Convert.ToInt32(reader[0]));

        var offset = (pageNumber - 1) * pageSize;
        var paginatedQuery = $"{baseQuery} ORDER BY 1 OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

        var data = await ExecuteQueryListAsync(paginatedQuery, parameters, mapFunction);

        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

        return new PagedResult<T>
        {
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1
        };
    }

    /// <summary>
    /// Ejecuta un stored procedure y retorna los datos mapeados junto con los parámetros de salida.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="inputParameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="outputParameters">Diccionario de parámetros de salida (nombre, tipo SQL).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto <typeparamref name="T"/>.</param>
    /// <returns>SqlOutputResult con los datos mapeados y los parámetros de salida.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    /// <example>
    /// var result = await SqlHelper.ExecuteStoredProcedureWithOutputAsync("sp_GetMedicosConTotal", new Dictionary<string, object> { ... }, new Dictionary<string, SqlDbType> { { "@TotalRecords", SqlDbType.Int } }, row => ...);
    /// </example>
    public static async Task<SqlOutputResult<T>> ExecuteStoredProcedureWithOutputAsync<T>(string storedProcedureName,Dictionary<string, object>? inputParameters, Dictionary<string, SqlDbType>? outputParameters,Func<DataRow, T>? mapFunction)
    {
        try
        {
            EnsureConfigured();
            var result = new SqlOutputResult<T>
            {
                Data = new List<T>(),
                OutputParameters = new Dictionary<string, object>()
            };
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            // Agregar parámetros de entrada
            AddParameters(command, inputParameters);

            // Agregar parámetros de salida
            if (outputParameters != null)
            {
                foreach (var param in outputParameters)
                {
                    var outputParam = new SqlParameter(param.Key, param.Value)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);
                }
            }

            var dataTable = new DataTable();
            var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
            int rowIndex = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                try
                {
                    result.Data.Add(mapFunction!(row));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error al mapear la fila {rowIndex} del resultado en '{storedProcedureName}': {ex.Message}");
                }
                rowIndex++;
            }

            // Obtener valores de parámetros de salida
            if (outputParameters != null)
            {
                foreach (var param in outputParameters)
                {
                    result.OutputParameters[param.Key] = command.Parameters[param.Key].Value;
                }
            }
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            if (ex.Message.StartsWith("Error al mapear"))
                throw;
            throw new Exception($"Error al ejecutar el stored procedure con parámetros de salida '{storedProcedureName}': {ex.Message}", ex);
        }
    }
    
    
    /// <summary>
    /// Agrega los parámetros al comando SQL, manejando posibles errores.
    /// </summary>
    /// <param name="command">Comando SQL al que se agregan los parámetros.</param>
    /// <param name="parameters">Diccionario de parámetros (nombre, valor).</param>
    private static void AddParameters(SqlCommand command, Dictionary<string, object>? parameters)
    {

        try
        {
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(new SqlParameter(param.Key, param.Value ?? DBNull.Value));
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al agregar parámetros al comando SQL: {ex.Message}", ex);
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
