# SqlServerHelper

**SqlServerHelper** es una librería .NET que simplifica el acceso a SQL Server, reemplazando la verbosidad de ADO.NET clásico con una API moderna y asíncrona.

## Objetivos
- Centralizar el acceso a datos SQL Server en proyectos .NET.
- Facilitar operaciones comunes (SELECT, INSERT, UPDATE, DELETE) y avanzadas (paginación, múltiples tablas, parámetros de salida).
- Mejorar la productividad y la mantenibilidad del código.

## Ventajas
- Ejecuta Stored Procedures y consultas SQL directamente.
- Soporta parámetros de entrada y salida.
- Ofrece paginación nativa (`PagedResult<T>`).
- Permite obtener múltiples tablas en un `DataSet`.
- Diseñado para uso intensivo en APIs y servicios backend.

Ideal para proyectos donde necesitas centralizar el acceso a datos sin depender de un ORM pesado como Entity Framework.