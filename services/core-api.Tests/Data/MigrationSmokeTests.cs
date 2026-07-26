using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RenderVN.CoreApi.Data;

namespace RenderVN.CoreApi.Tests.Data;

public sealed class MigrationSmokeTests
{
    [Fact]
    public void GeneratedMigrationsIncludeInitialSchemaAndConcurrencyUpdate()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new AppDbContext(options);

        var migrations = db.Database.GetMigrations().ToArray();

        Assert.Contains(migrations, migration => migration.EndsWith("_InitialCreate"));
        Assert.Contains(migrations, migration => migration.EndsWith("_AddCreditWalletConcurrency"));
    }
}
