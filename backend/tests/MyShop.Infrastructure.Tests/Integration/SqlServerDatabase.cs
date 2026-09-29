using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence;

namespace MyShop.Infrastructure.Tests.Integration;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerDatabase.EnvironmentVariable)))
            Skip = $"Set {SqlServerDatabase.EnvironmentVariable} to run isolated SQL Server integration tests.";
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerDatabase>
{
    public const string Name = "SQL Server integration";
}

public sealed class SqlServerDatabase : IAsyncLifetime
{
    public const string EnvironmentVariable = "MYSHOP_TEST_SQLSERVER";
    private readonly string _databaseName = "MyShopTests_" + Guid.NewGuid().ToString("N");
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured)) return;
        // Ignore any supplied database: tests may create/delete only their own randomly named database.
        var builder = new SqlConnectionStringBuilder(configured) { InitialCatalog = _databaseName };
        if (!string.IsNullOrWhiteSpace(builder.AttachDBFilename))
            throw new InvalidOperationException("SQL Server integration tests require a server connection without AttachDBFilename.");
        _connectionString = builder.ConnectionString;
        try
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public MyShopDbContext CreateContext()
    {
        if (_connectionString is null)
            throw new InvalidOperationException($"Set {EnvironmentVariable} before running SQL Server tests.");
        return new MyShopDbContext(new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer(_connectionString).Options);
    }

    public async Task DisposeAsync()
    {
        if (_connectionString is null) return;
        var target = new SqlConnectionStringBuilder(_connectionString);
        if (target.InitialCatalog != _databaseName
            || !_databaseName.StartsWith("MyShopTests_", StringComparison.Ordinal)
            || !Guid.TryParseExact(_databaseName["MyShopTests_".Length..], "N", out _))
            throw new InvalidOperationException("Refusing to delete a database not owned by this test fixture.");
        using var connection = new SqlConnection(_connectionString);
        SqlConnection.ClearPool(connection);
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        _connectionString = null;
    }
}
