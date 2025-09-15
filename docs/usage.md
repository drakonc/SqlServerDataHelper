# Ejemplos de uso de cada método

## 1. ExecuteQueryListAsync
Obtiene una lista de objetos mapeados desde una consulta SQL:
```csharp
var medicos = await SqlServerHelper.ExecuteQueryListAsync(
	"SELECT Id, Nombre FROM Medico",
	null,
	reader => new Medico {
		Id = reader.GetInt32(0),
		Nombre = reader.GetString(1)
	});
```

## 2. ExecuteQuerySingleAsync
Obtiene un solo objeto mapeado desde una consulta SQL:
```csharp
var medico = await SqlServerHelper.ExecuteQuerySingleAsync(
	"SELECT Id, Nombre FROM Medico WHERE Id = @Id",
	new() { { "@Id", 1 } },
	reader => new Medico {
		Id = reader.GetInt32(0),
		Nombre = reader.GetString(1)
	});
```

## 3. ExecuteQueryAsync
Ejecuta una consulta SQL que no retorna resultados (INSERT, UPDATE, DELETE):
```csharp
await SqlServerHelper.ExecuteQueryAsync(
	"UPDATE Medico SET Nombre = @Nombre WHERE Id = @Id",
	new() {
		{ "@Id", 1 },
		{ "@Nombre", "Pedro" }
	});
```

## 4. ExecuteStoredProcedureListAsync
Obtiene una lista de objetos mapeados desde un Stored Procedure:
```csharp
var medicos = await SqlServerHelper.ExecuteStoredProcedureListAsync(
	"sp_GetMedicos",
	null,
	reader => new Medico {
		Id = reader.GetInt32(0),
		Nombre = reader.GetString(1)
	});
```

## 5. ExecuteStoredProcedureSingleAsync
Obtiene un solo objeto mapeado desde un Stored Procedure:
```csharp
var medico = await SqlServerHelper.ExecuteStoredProcedureSingleAsync(
	"sp_GetMedico",
	new() { { "@Id", 1 } },
	reader => new Medico {
		Id = reader.GetInt32(0),
		Nombre = reader.GetString(1)
	});
```

## 6. ExecuteStoredProcedureAsync
Ejecuta un Stored Procedure que no retorna resultados:
```csharp
await SqlServerHelper.ExecuteStoredProcedureAsync(
	"sp_InsertMedico",
	new() {
		{ "@Nombre", "Juan" },
		{ "@Especialidad", "Cardiología" }
	});
```

## 7. ExecuteStoredProcedureWithOutputAsync
Ejecuta un Stored Procedure con parámetros de salida:
```csharp
var result = await SqlServerHelper.ExecuteStoredProcedureWithOutputAsync(
	"sp_GetTotalMedicos",
	null,
	new Dictionary<string, SqlDbType> { { "@Total", SqlDbType.Int } },
	reader => new Medico {
		Id = reader.GetInt32(0),
		Nombre = reader.GetString(1)
	});
int total = (int)result.OutputParameters["@Total"];
```

## 8. ExecuteStoredProcedureMultipleTablesAsync
Obtiene múltiples tablas en un DataSet desde un Stored Procedure:
```csharp
var ds = await SqlServerHelper.ExecuteStoredProcedureMultipleTablesAsync(
	"sp_GetMedicosAndConsultorios");
```

## 9. ExecuteQueryMultipleTablesAsync
Obtiene múltiples tablas en un DataSet desde una consulta SQL:
```csharp
var ds = await SqlServerHelper.ExecuteQueryMultipleTablesAsync(
	"SELECT * FROM Medico; SELECT * FROM Consultorio");
```

## 10. ExecuteStoredProcedurePaginatedAsync
Obtiene datos paginados desde un Stored Procedure:
```csharp
var pagedMedicos = await SqlServerHelper.ExecuteStoredProcedurePaginatedAsync(
	"sp_GetMedicosPaged",
	pageNumber: 1,
	pageSize: 10,
	parameters: null,
	reader => new Medico {
		Id = reader.GetInt32(0),
		Nombre = reader.GetString(1)
	});
```

## 11. ExecuteQueryPaginatedAsync
Obtiene datos paginados desde una consulta SQL:
```csharp
var pagedMedicos = await SqlServerHelper.ExecuteQueryPaginatedAsync(
	"SELECT Id, Nombre FROM Medico",
	"SELECT COUNT(*) FROM Medico",
	pageNumber: 1,
	pageSize: 10,
	parameters: null,
	reader => new Medico {
		Id = reader.GetInt32(0),
		Nombre = reader.GetString(1)
	});
```