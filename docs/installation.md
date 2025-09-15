# Instalación

1. Copia el proyecto `SqlHelper` dentro de tu solución.
2. Agrega referencia al proyecto en tu `Api` o capa donde lo necesites:

```xml
<ProjectReference Include="..\SqlHelper\SqlHelper.csproj" />
```

3. Configura la cadena de conexión en `Program.cs`:

```csharp
using SqlServerHelper;

var builder = WebApplication.CreateBuilder(args);
SqlServerHelper.Configure(builder.Configuration.GetConnectionString("DefaultConnection"));
```

4. Ya puedes utilizar SqlServerHelper en tus services y controllers:

```csharp
var medicos = await SqlServerHelper.ExecuteQueryListAsync(
	"SELECT * FROM Medico",
	null,
	reader => new Medico { Id = reader.GetInt32(0), Nombre = reader.GetString(1) });
```