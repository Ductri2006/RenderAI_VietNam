using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RenderVN.CoreApi.Data;
using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Tests.Domain;

public sealed class CreditLedgerTests
{
    [Fact]
    public async Task GrantAddsTwentyAvailableCreditsAndLedgerRow()
    {
        await using var fixture = await LedgerFixture.CreateAsync();

        var result = await fixture.Ledger.GrantAsync(fixture.Wallet.Id, 20, "signup-grant");

        Assert.True(result.IsSuccess);

        var wallet = await fixture.Db.CreditWallets.SingleAsync();
        Assert.Equal(20, wallet.AvailableCredits);
        Assert.Equal(0, wallet.ReservedCredits);

        var transaction = await fixture.Db.CreditTransactions.SingleAsync();
        Assert.Equal(CreditTransactionType.Grant, transaction.Type);
        Assert.Equal(20, transaction.AvailableDelta);
        Assert.Equal(0, transaction.ReservedDelta);
        Assert.Equal("signup-grant", transaction.IdempotencyKey);
    }

    [Fact]
    public async Task ReserveMovesFourCreditsFromAvailableToReserved()
    {
        await using var fixture = await LedgerFixture.CreateAsync(availableCredits: 20);

        var result = await fixture.Ledger.ReserveAsync(fixture.Wallet.Id, 4, "render-reserve");

        Assert.True(result.IsSuccess);

        var wallet = await fixture.Db.CreditWallets.SingleAsync();
        Assert.Equal(16, wallet.AvailableCredits);
        Assert.Equal(4, wallet.ReservedCredits);

        var transaction = await fixture.Db.CreditTransactions.SingleAsync();
        Assert.Equal(CreditTransactionType.Reserve, transaction.Type);
        Assert.Equal(-4, transaction.AvailableDelta);
        Assert.Equal(4, transaction.ReservedDelta);
    }

    [Fact]
    public async Task ConsumeClearsReservedCreditsWithoutChangingAvailableCredits()
    {
        await using var fixture = await LedgerFixture.CreateAsync(
            availableCredits: 16,
            reservedCredits: 4);

        var result = await fixture.Ledger.ConsumeAsync(fixture.Wallet.Id, 4, "render-consume");

        Assert.True(result.IsSuccess);

        var wallet = await fixture.Db.CreditWallets.SingleAsync();
        Assert.Equal(16, wallet.AvailableCredits);
        Assert.Equal(0, wallet.ReservedCredits);

        var transaction = await fixture.Db.CreditTransactions.SingleAsync();
        Assert.Equal(CreditTransactionType.Consume, transaction.Type);
        Assert.Equal(0, transaction.AvailableDelta);
        Assert.Equal(-4, transaction.ReservedDelta);
    }

    [Fact]
    public async Task RefundMovesReservedCreditsBackToAvailable()
    {
        await using var fixture = await LedgerFixture.CreateAsync(
            availableCredits: 16,
            reservedCredits: 4);

        var result = await fixture.Ledger.RefundAsync(fixture.Wallet.Id, 4, "render-refund");

        Assert.True(result.IsSuccess);

        var wallet = await fixture.Db.CreditWallets.SingleAsync();
        Assert.Equal(20, wallet.AvailableCredits);
        Assert.Equal(0, wallet.ReservedCredits);

        var transaction = await fixture.Db.CreditTransactions.SingleAsync();
        Assert.Equal(CreditTransactionType.Refund, transaction.Type);
        Assert.Equal(4, transaction.AvailableDelta);
        Assert.Equal(-4, transaction.ReservedDelta);
    }

    [Fact]
    public async Task SameIdempotencyKeyCannotReserveTwice()
    {
        await using var fixture = await LedgerFixture.CreateAsync(availableCredits: 20);

        var first = await fixture.Ledger.ReserveAsync(fixture.Wallet.Id, 4, "same-operation");
        var second = await fixture.Ledger.ReserveAsync(fixture.Wallet.Id, 4, "same-operation");

        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("duplicate_idempotency_key", second.ErrorCode);

        var wallet = await fixture.Db.CreditWallets.SingleAsync();
        Assert.Equal(16, wallet.AvailableCredits);
        Assert.Equal(4, wallet.ReservedCredits);
        Assert.Equal(1, await fixture.Db.CreditTransactions.CountAsync());
    }

    [Fact]
    public async Task InsufficientCreditsReturnsDomainErrorWithoutLedgerMutation()
    {
        await using var fixture = await LedgerFixture.CreateAsync(availableCredits: 3);

        var result = await fixture.Ledger.ReserveAsync(fixture.Wallet.Id, 4, "too-expensive");

        Assert.False(result.IsSuccess);
        Assert.Equal("insufficient_credits", result.ErrorCode);

        var wallet = await fixture.Db.CreditWallets.SingleAsync();
        Assert.Equal(3, wallet.AvailableCredits);
        Assert.Equal(0, wallet.ReservedCredits);
        Assert.Empty(await fixture.Db.CreditTransactions.ToListAsync());
    }

    [Fact]
    public async Task PersistedLedgerRowsCannotBeModified()
    {
        await using var fixture = await LedgerFixture.CreateAsync();
        await fixture.Ledger.GrantAsync(fixture.Wallet.Id, 20, "immutable-grant");
        var transaction = await fixture.Db.CreditTransactions.SingleAsync();

        transaction.AvailableDelta = 999;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Db.SaveChangesAsync());
        Assert.Equal("Credit transactions are immutable.", exception.Message);
    }

    [Fact]
    public async Task PersistedLedgerRowsCannotBeDeletedWithSynchronousSave()
    {
        await using var fixture = await LedgerFixture.CreateAsync();
        await fixture.Ledger.GrantAsync(fixture.Wallet.Id, 20, "immutable-delete");
        var transaction = await fixture.Db.CreditTransactions.SingleAsync();

        fixture.Db.CreditTransactions.Remove(transaction);

        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Db.SaveChanges());
        Assert.Equal("Credit transactions are immutable.", exception.Message);
    }

    private sealed class LedgerFixture : IAsyncDisposable
    {
        private LedgerFixture(
            SqliteConnection connection,
            AppDbContext db,
            CreditWallet wallet)
        {
            Connection = connection;
            Db = db;
            Wallet = wallet;
            Ledger = new CreditLedger(db);
        }

        public SqliteConnection Connection { get; }
        public AppDbContext Db { get; }
        public CreditWallet Wallet { get; }
        public CreditLedger Ledger { get; }

        public static async Task<LedgerFixture> CreateAsync(
            int availableCredits = 0,
            int reservedCredits = 0)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "ledger@example.com",
                Email = "ledger@example.com"
            };
            var wallet = new CreditWallet
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                AvailableCredits = availableCredits,
                ReservedCredits = reservedCredits
            };
            db.AddRange(user, wallet);
            await db.SaveChangesAsync();

            return new LedgerFixture(connection, db, wallet);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
