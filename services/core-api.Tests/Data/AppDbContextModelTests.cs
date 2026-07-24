using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RenderVN.CoreApi.Data;
using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Tests.Data;

public sealed class AppDbContextModelTests
{
    [Fact]
    public void RequiredEntitiesUseGuidKeysAndUtcCreatedTimestamps()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new AppDbContext(options);

        Type[] entityTypes =
        [
            typeof(ApplicationUser),
            typeof(Project),
            typeof(SourceImage),
            typeof(RenderJob),
            typeof(RenderResult),
            typeof(CreditWallet),
            typeof(CreditTransaction),
            typeof(PaymentOrder),
            typeof(StylePreset),
            typeof(AuditEvent)
        ];

        foreach (var clrType in entityTypes)
        {
            var entity = db.Model.FindEntityType(clrType);
            Assert.NotNull(entity);
            Assert.Equal(typeof(Guid), entity.FindPrimaryKey()!.Properties.Single().ClrType);
            Assert.Equal(typeof(DateTimeOffset), entity.FindProperty("CreatedAt")!.ClrType);
        }
    }

    [Fact]
    public void RequiredUniqueAndQueryIndexesAreConfigured()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new AppDbContext(options);

        AssertIndex<ApplicationUser>(db.Model, true, "NormalizedEmail");
        AssertIndex<CreditWallet>(db.Model, true, "UserId");
        AssertIndex<RenderJob>(db.Model, false, "UserId", "CreatedAt");
        AssertIndex<CreditTransaction>(db.Model, false, "WalletId", "CreatedAt");
        AssertIndex<CreditTransaction>(db.Model, true, "WalletId", "IdempotencyKey");
    }

    private static void AssertIndex<TEntity>(
        IModel model,
        bool unique,
        params string[] propertyNames)
    {
        var entity = model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        var index = entity.GetIndexes().SingleOrDefault(candidate =>
            candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
        Assert.NotNull(index);
        Assert.Equal(unique, index.IsUnique);
    }
}
