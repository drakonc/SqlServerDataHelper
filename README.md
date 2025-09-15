# SqlServerDataHelper

SqlServerDataHelper es una librería .NET pensada para facilitar el acceso y manejo de datos en SQL Server, especialmente en proyectos donde se requiere un enfoque directo, seguro y eficiente sin depender de ORM pesados como Entity Framework.

## ¿Qué resuelve?
Permite centralizar la lógica de acceso a datos, simplificando operaciones comunes (SELECT, INSERT, UPDATE, DELETE) y avanzadas (paginación, múltiples tablas, parámetros de salida) usando una API moderna y asíncrona.

## Características principales
- Ejecución directa de Stored Procedures y consultas SQL.
- Soporte para parámetros de entrada y salida.
- Paginación nativa (`PagedResult<T>`).
- Obtención de múltiples tablas en un `DataSet`.
- Manejo robusto de errores y excepciones.
- Documentación y ejemplos claros para cada función.

## Ejemplo de uso
```csharp
var medicos = await SqlServerHelper.ExecuteQueryListAsync(
	"SELECT Id, Nombre FROM Medico",
	null,
	reader => new Medico {
		Id = reader.GetInt32(0),
		Nombre = reader.GetString(1)
	});
```

## Estructura del proyecto
- **Api/**: Proyecto principal de la API.
- **SqlServerHelper/**: Librería de acceso a datos.
- **Tests/**: Pruebas unitarias y de integración.
- **docs/**: Documentación detallada, ejemplos y scripts.

## ¿Cuándo usar esta librería?
- Cuando ya tienes procedimientos almacenados y quieres máximo rendimiento.
- Si buscas evitar la sobrecarga de un ORM.
- Para centralizar y estandarizar el acceso a SQL Server en tus proyectos .NET.

## Más información
Consulta la carpeta `docs/` para ver ejemplos avanzados, guía de migración, preguntas frecuentes y scripts de prueba.