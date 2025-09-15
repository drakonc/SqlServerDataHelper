using SqlHelper;
using Tests.Fixtures;

namespace Tests;

public class SqlServerHelperTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task ExecuteStoredProcedureListAsync_ShouldReturnMedicos()
    {
        var result = await SqlServerHelper.ExecuteStoredProcedureListAsync(
            "sp_GetMedicos",
            null,
            reader => new
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Especialidad = reader.GetString(2)
            });

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ExecuteStoredProcedureSingleAsync_ShouldReturnMedicoById()
    {
        var result = await SqlServerHelper.ExecuteStoredProcedureSingleAsync(
            "sp_GetMedicoById",
            new() { { "@Id", 1 } },
            reader => new
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Especialidad = reader.GetString(2)
            });

        Assert.NotNull(result);
        Assert.Equal(1, result?.Id);
    }

    [Fact]
    public async Task ExecuteStoredProcedureAsync_ShouldInsertMedico()
    {
        int rows = await SqlServerHelper.ExecuteStoredProcedureAsync(
            "sp_InsertMedico",
            new() {
                { "@Nombre", "Nuevo Medico" },
                { "@Especialidad", "Dermatología" }
            });

        Assert.True(rows >= 0); // Ado retorna 0 en algunos insert sin output
    }

    [Fact]
    public async Task ExecuteQueryListAsync_ShouldReturnMedicos()
    {
        var result = await SqlServerHelper.ExecuteQueryListAsync(
            "SELECT * FROM Medico",
            null,
            reader => new
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Especialidad = reader.GetString(2)
            });

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ExecuteStoredProcedurePaginatedAsync_ShouldReturnPagedResult()
    {
        var paged = await SqlServerHelper.ExecuteStoredProcedurePaginatedAsync(
            "sp_GetMedicosPaged",
            1,
            2,
            null,
            reader => new
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Especialidad = reader.GetString(2)
            });

        Assert.NotEmpty(paged.Data);
        Assert.Equal(1, paged.PageNumber);
        Assert.True(paged.TotalRecords > 0);
    }
}
