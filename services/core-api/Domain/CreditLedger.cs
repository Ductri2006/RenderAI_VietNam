using Microsoft.EntityFrameworkCore;
using RenderVN.CoreApi.Data;

namespace RenderVN.CoreApi.Domain;

public sealed class CreditLedger(AppDbContext db)
{
    public async Task<CreditLedgerResult> GrantAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var wallet = await db.CreditWallets
            .SingleAsync(item => item.Id == walletId, cancellationToken);

        wallet.AvailableCredits += credits;
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        db.CreditTransactions.Add(new CreditTransaction
        {
            WalletId = walletId,
            Type = CreditTransactionType.Grant,
            AvailableDelta = credits,
            IdempotencyKey = idempotencyKey
        });

        await db.SaveChangesAsync(cancellationToken);
        return CreditLedgerResult.Success();
    }

    public async Task<CreditLedgerResult> ReserveAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var alreadyApplied = await db.CreditTransactions.AnyAsync(
            transaction => transaction.WalletId == walletId
                && transaction.IdempotencyKey == idempotencyKey,
            cancellationToken);
        if (alreadyApplied)
        {
            return CreditLedgerResult.Failure("duplicate_idempotency_key");
        }

        var wallet = await db.CreditWallets
            .SingleAsync(item => item.Id == walletId, cancellationToken);
        if (wallet.AvailableCredits < credits)
        {
            return CreditLedgerResult.Failure("insufficient_credits");
        }

        wallet.AvailableCredits -= credits;
        wallet.ReservedCredits += credits;
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        db.CreditTransactions.Add(new CreditTransaction
        {
            WalletId = walletId,
            Type = CreditTransactionType.Reserve,
            AvailableDelta = -credits,
            ReservedDelta = credits,
            IdempotencyKey = idempotencyKey
        });

        await db.SaveChangesAsync(cancellationToken);
        return CreditLedgerResult.Success();
    }

    public async Task<CreditLedgerResult> ConsumeAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var wallet = await db.CreditWallets
            .SingleAsync(item => item.Id == walletId, cancellationToken);

        wallet.ReservedCredits -= credits;
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        db.CreditTransactions.Add(new CreditTransaction
        {
            WalletId = walletId,
            Type = CreditTransactionType.Consume,
            ReservedDelta = -credits,
            IdempotencyKey = idempotencyKey
        });

        await db.SaveChangesAsync(cancellationToken);
        return CreditLedgerResult.Success();
    }

    public async Task<CreditLedgerResult> RefundAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var wallet = await db.CreditWallets
            .SingleAsync(item => item.Id == walletId, cancellationToken);

        wallet.AvailableCredits += credits;
        wallet.ReservedCredits -= credits;
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        db.CreditTransactions.Add(new CreditTransaction
        {
            WalletId = walletId,
            Type = CreditTransactionType.Refund,
            AvailableDelta = credits,
            ReservedDelta = -credits,
            IdempotencyKey = idempotencyKey
        });

        await db.SaveChangesAsync(cancellationToken);
        return CreditLedgerResult.Success();
    }
}
