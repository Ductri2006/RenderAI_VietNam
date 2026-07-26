using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RenderVN.CoreApi.Data;
using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Tests.Domain;

public sealed class CreditConcurrencyTests
{
    [Fact]
    public async Task ConflictingWalletUpdatesReturnConcurrencyDomainError()
    {
        const string connectionString = "Data Source=file:credit-concurrency;Mode=Memory;Cache=Shared";
        await using var connection1 = new SqliteConnection(connectionString);
        await connection1.OpenAsync();
        var options1 = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection1)
            .Options;
        await using var db1 = new AppDbContext(options1);
        await db1.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "concurrency@example.com",
            Email = "concurrency@example.com"
        };
        var wallet = new CreditWallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            AvailableCredits = 20
        };
        db1.AddRange(user, wallet);
        await db1.SaveChangesAsync();

        await using var connection2 = new SqliteConnection(connectionString);
        await connection2.OpenAsync();
        var options2 = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection2)
            .Options;
        await using var db2 = new AppDbContext(options2);
        await db1.CreditWallets.SingleAsync();
        await db2.CreditWallets.SingleAsync();
        var pendingProject = new Project
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Pending unrelated project",
            RoomType = RoomType.LivingRoom
        };
        db2.Projects.Add(pendingProject);

        var first = await new CreditLedger(db1).ReserveAsync(wallet.Id, 4, "first-operation");
        var second = await new CreditLedger(db2).ReserveAsync(wallet.Id, 4, "second-operation");

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("concurrency_conflict", second.ErrorCode);

        var persisted = await db1.CreditWallets.SingleAsync(item => item.Id == wallet.Id);
        Assert.Equal(16, persisted.AvailableCredits);
        Assert.Equal(4, persisted.ReservedCredits);
        Assert.Equal(1, await db1.CreditTransactions.CountAsync());
        Assert.Equal(EntityState.Added, db2.Entry(pendingProject).State);
        await db2.SaveChangesAsync();
        Assert.True(await db1.Projects.AnyAsync(project => project.Id == pendingProject.Id));
    }
}
