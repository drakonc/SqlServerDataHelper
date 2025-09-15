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
    /// SqlServerHelper.Configure("Server=myServer;Database=myDb;User Id=myUser;Password=myPass;");
    /// </example>
    public static void Configure(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("La cadena de conexión no puede estar vacía", nameof(connectionString));

        _connectionString = connectionString;
    }

    // ====
    // 1. STORED PROCEDURES - FUNCIONES BÁSICAS
    // ====

    /// <summary>
    /// Ejecuta un stored procedure que retorna una lista de objetos mapeados por mapFunction.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto T.</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>Lista de objetos mapeados.</returns>
    /// <exception cref="InvalidOperationException">Si la cadena de conexión no está configurada.</exception>
    public static async Task<List<T>> ExecuteStoredProcedureListAsync<T>(string storedProcedureName, Dictionary<string, object>? parameters = null,Func<IDataRecord, T>? mapFunction = null,CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConfigured();
            ArgumentNullException.ThrowIfNull(mapFunction);

            var result = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(command, parameters);
            await connection.OpenAsync(cancellationToken);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            int rowIndex = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    result.Add(mapFunction(reader));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error al mapear la fila {rowIndex} del resultado en '{storedProcedureName}': {ex.Message}", ex);
                }
                rowIndex++;
            }

            return result;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("Error al mapear")))
        {
            throw new InvalidOperationException($"Error al ejecutar el stored procedure '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta un stored procedure y retorna el primer resultado mapeado por mapFunction.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="mapFunction">Función para mapear la fila del resultado a un objeto T.</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>Primer objeto mapeado o default si no hay resultados.</returns>
    public static async Task<T?> ExecuteStoredProcedureSingleAsync<T>(string storedProcedureName, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConfigured();
            ArgumentNullException.ThrowIfNull(mapFunction);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(command, parameters);
            await connection.OpenAsync(cancellationToken);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    return mapFunction(reader);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error al mapear el resultado en '{storedProcedureName}': {ex.Message}", ex);
                }
            }

            return default;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("Error al mapear")))
        {
            throw new InvalidOperationException($"Error al ejecutar el stored procedure '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta un stored procedure que no retorna resultados (INSERT, UPDATE, DELETE).
    /// </summary>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>Número de filas afectadas.</returns>
    public static async Task<int> ExecuteStoredProcedureAsync(string storedProcedureName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
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
            await connection.OpenAsync(cancellationToken);

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al ejecutar el stored procedure '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    // ====
    // 2. CONSULTAS SQL DIRECTAS
    // ====

    /// <summary>
    /// Ejecuta una consulta SQL que retorna una lista de objetos mapeados por mapFunction.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto T.</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>Lista de objetos mapeados.</returns>
    public static async Task<List<T>> ExecuteQueryListAsync<T>(string sqlQuery, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConfigured();
            ArgumentNullException.ThrowIfNull(mapFunction);

            var result = new List<T>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection);

            AddParameters(command, parameters);
            await connection.OpenAsync(cancellationToken);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            int rowIndex = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    result.Add(mapFunction(reader));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error al mapear la fila {rowIndex} del resultado en la consulta '{sqlQuery}': {ex.Message}", ex);
                }
                rowIndex++;
            }

            return result;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("Error al mapear")))
        {
            throw new InvalidOperationException($"Error al ejecutar la consulta SQL '{sqlQuery}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL y retorna el primer resultado mapeado por mapFunction.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <param name="mapFunction">Función para mapear la fila del resultado a un objeto T.</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>Primer objeto mapeado o default si no hay resultados.</returns>
    public static async Task<T?> ExecuteQuerySingleAsync<T>(string sqlQuery, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConfigured();
            ArgumentNullException.ThrowIfNull(mapFunction);

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection);

            AddParameters(command, parameters);
            await connection.OpenAsync(cancellationToken);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    return mapFunction(reader);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error al mapear el resultado en la consulta '{sqlQuery}': {ex.Message}", ex);
                }
            }

            return default;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("Error al mapear")))
        {
            throw new InvalidOperationException($"Error al ejecutar la consulta SQL '{sqlQuery}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL que no retorna resultados (INSERT, UPDATE, DELETE).
    /// </summary>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>Número de filas afectadas por la consulta.</returns>
    public static async Task<int> ExecuteQueryAsync(string sqlQuery, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConfigured();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sqlQuery, connection);

            AddParameters(command, parameters);
            await connection.OpenAsync(cancellationToken);

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al ejecutar la consulta SQL '{sqlQuery}': {ex.Message}", ex);
        }
    }

    // ====
    // 3. MÚLTIPLES TABLAS (DataSet) - Mantiene DataTable por necesidad
    // ====

    /// <summary>
    /// Ejecuta un stored procedure que retorna múltiples tablas en un DataSet.
    /// </summary>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <returns>DataSet con las tablas retornadas.</returns>
    public static async Task<DataSet> ExecuteStoredProcedureMultipleTablesAsync(string storedProcedureName, Dictionary<string, object>? parameters = null)
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

            var dataSet = new DataSet();
            var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataSet);

            return await Task.FromResult(dataSet);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al ejecutar el stored procedure '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL que retorna múltiples tablas en un DataSet.
    /// </summary>
    /// <param name="sqlQuery">Consulta SQL a ejecutar.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <returns>DataSet con las tablas retornadas.</returns>
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
            throw new InvalidOperationException($"Error al ejecutar la consulta SQL '{sqlQuery}': {ex.Message}", ex);
        }
    }

    // ====
    // 4. PARÁMETROS DE SALIDA
    // ====

    /// <summary>
    /// Ejecuta un stored procedure con parámetros de salida.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="inputParameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="outputParameters">Diccionario de parámetros de salida (nombre, tipo SQL).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto T.</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>SqlOutputResult con los datos mapeados y los parámetros de salida.</returns>
    public static async Task<SqlOutputResult<T>> ExecuteStoredProcedureWithOutputAsync<T>(string storedProcedureName, Dictionary<string, object>? inputParameters = null, Dictionary<string, SqlDbType>? outputParameters = null, Func<IDataRecord, T>? mapFunction = null, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConfigured();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Parámetros de entrada
            AddParameters(command, inputParameters);

            // Parámetros de salida
            var outputParams = new Dictionary<string, SqlParameter>();
            if (outputParameters != null)
            {
                foreach (var param in outputParameters)
                {
                    var sqlParam = new SqlParameter(param.Key, param.Value)
                    {
                        Direction = ParameterDirection.Output,
                        Size = param.Value == SqlDbType.NVarChar ? 4000 : 0
                    };
                    command.Parameters.Add(sqlParam);
                    outputParams[param.Key] = sqlParam;
                }
            }

            await connection.OpenAsync(cancellationToken);

            var data = new List<T>();

            // Si hay mapFunction, usar reader para obtener datos
            if (mapFunction != null)
            {
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                int rowIndex = 0;
                while (await reader.ReadAsync(cancellationToken))
                {
                    try
                    {
                        data.Add(mapFunction(reader));
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error al mapear la fila {rowIndex} del resultado en '{storedProcedureName}': {ex.Message}", ex);
                    }
                    rowIndex++;
                }
            }
            else
            {
                // Solo ejecutar sin leer datos
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            // Extraer valores de salida
            var outputValues = new Dictionary<string, object>();
            foreach (var param in outputParams)
            {
                outputValues[param.Key] = param.Value.Value ?? DBNull.Value;
            }

            return new SqlOutputResult<T>
            {
                Data = data,
                OutputParameters = outputValues
            };
        }
        catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("Error al mapear")))
        {
            throw new InvalidOperationException($"Error al ejecutar el stored procedure con parámetros de salida '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    // ====
    // 5. PAGINACIÓN
    // ====

    /// <summary>
    /// Ejecuta un stored procedure con paginación automática.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="storedProcedureName">Nombre del procedimiento almacenado.</param>
    /// <param name="pageNumber">Número de página (base 1).</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="parameters">Parámetros de entrada para el procedimiento (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto T.</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>PagedResult con los datos y la información de paginación.</returns>
    public static async Task<PagedResult<T>> ExecuteStoredProcedurePaginatedAsync<T>(string storedProcedureName, int pageNumber, int pageSize, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConfigured();
            ArgumentNullException.ThrowIfNull(mapFunction);

            if (pageNumber < 1) throw new ArgumentException("El número de página debe ser mayor a 0", nameof(pageNumber));
            if (pageSize < 1) throw new ArgumentException("El tamaño de página debe ser mayor a 0", nameof(pageSize));

            var allParameters = new Dictionary<string, object>(parameters ?? new())
            {
                { "@PageNumber", pageNumber },
                { "@PageSize", pageSize }
            };

            var outputParams = new Dictionary<string, SqlDbType>
            {
                { "@TotalRecords", SqlDbType.Int }
            };

            var result = await ExecuteStoredProcedureWithOutputAsync(storedProcedureName, allParameters, outputParams, mapFunction, cancellationToken);

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
        catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("Error al mapear")))
        {
            throw new InvalidOperationException($"Error al ejecutar el stored procedure paginado '{storedProcedureName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL con paginación manual.
    /// </summary>
    /// <typeparam name="T">Tipo de objeto a mapear.</typeparam>
    /// <param name="baseQuery">Consulta base para obtener los datos.</param>
    /// <param name="countQuery">Consulta para obtener el total de registros.</param>
    /// <param name="pageNumber">Número de página (base 1).</param>
    /// <param name="pageSize">Tamaño de página.</param>
    /// <param name="parameters">Parámetros de entrada para la consulta (opcional).</param>
    /// <param name="mapFunction">Función para mapear cada fila del resultado a un objeto T.</param>
    /// <param name="cancellationToken">Token de cancelación (opcional).</param>
    /// <returns>PagedResult con los datos y la información de paginación.</returns>
    public static async Task<PagedResult<T>> ExecuteQueryPaginatedAsync<T>(string baseQuery, string countQuery, int pageNumber, int pageSize, Dictionary<string, object>? parameters = null, Func<IDataRecord, T>? mapFunction = null, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConfigured();
            ArgumentNullException.ThrowIfNull(mapFunction);

            if (pageNumber < 1) throw new ArgumentException("El número de página debe ser mayor a 0", nameof(pageNumber));
            if (pageSize < 1) throw new ArgumentException("El tamaño de página debe ser mayor a 0", nameof(pageSize));

            // Obtener total de registros
            var totalRecords = await ExecuteQuerySingleAsync<int>(
                countQuery,
                parameters,
                reader => reader.GetInt32(0),
                cancellationToken);

            // Construir query paginado
            var offset = (pageNumber - 1) * pageSize;
            var paginatedQuery = $"{baseQuery} ORDER BY 1 OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            // Obtener datos paginados
            var data = await ExecuteQueryListAsync(paginatedQuery, parameters, mapFunction, cancellationToken);

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
        catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("Error al mapear")))
        {
            throw new InvalidOperationException($"Error al ejecutar la consulta paginada: {ex.Message}", ex);
        }
    }

    // ====
    // MÉTODOS AUXILIARES PRIVADOS
    // ====

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
            throw new InvalidOperationException("SqlServerHelper no está configurado. Llama a SqlServerHelper.Configure(connectionString) al inicio de la aplicación.");
    }
}