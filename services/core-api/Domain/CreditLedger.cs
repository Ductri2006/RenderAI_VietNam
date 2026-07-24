using Microsoft.EntityFrameworkCore;
using RenderVN.CoreApi.Data;

namespace RenderVN.CoreApi.Domain;

public sealed class CreditLedger(AppDbContext db)
{
    public Task<CreditLedgerResult> GrantAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return ApplyAsync(
            walletId,
            credits,
            idempotencyKey,
            CreditTransactionType.Grant,
            availableDelta: credits,
            reservedDelta: 0,
            cancellationToken);
    }

    public Task<CreditLedgerResult> ReserveAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return ApplyAsync(
            walletId,
            credits,
            idempotencyKey,
            CreditTransactionType.Reserve,
            availableDelta: -credits,
            reservedDelta: credits,
            cancellationToken);
    }

    public Task<CreditLedgerResult> ConsumeAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return ApplyAsync(
            walletId,
            credits,
            idempotencyKey,
            CreditTransactionType.Consume,
            availableDelta: 0,
            reservedDelta: -credits,
            cancellationToken);
    }

    public Task<CreditLedgerResult> RefundAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return ApplyAsync(
            walletId,
            credits,
            idempotencyKey,
            CreditTransactionType.Refund,
            availableDelta: credits,
            reservedDelta: -credits,
            cancellationToken);
    }

    private async Task<CreditLedgerResult> ApplyAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CreditTransactionType type,
        int availableDelta,
        int reservedDelta,
        CancellationToken cancellationToken)
    {
        if (credits <= 0)
        {
            return CreditLedgerResult.Failure("invalid_credit_amount");
        }

        if (await HasIdempotencyKeyAsync(walletId, idempotencyKey, cancellationToken))
        {
            return CreditLedgerResult.Failure("duplicate_idempotency_key");
        }

        var wallet = await db.CreditWallets
            .SingleAsync(item => item.Id == walletId, cancellationToken);

        if (type == CreditTransactionType.Reserve && wallet.AvailableCredits < credits)
        {
            return CreditLedgerResult.Failure("insufficient_credits");
        }

        if (type is CreditTransactionType.Consume or CreditTransactionType.Refund
            && wallet.ReservedCredits < credits)
        {
            return CreditLedgerResult.Failure("insufficient_reserved_credits");
        }

        wallet.AvailableCredits += availableDelta;
        wallet.ReservedCredits += reservedDelta;
        wallet.Version++;
        wallet.UpdatedAt = DateTimeOffset.UtcNow;
        db.CreditTransactions.Add(new CreditTransaction
        {
            WalletId = walletId,
            Type = type,
            AvailableDelta = availableDelta,
            ReservedDelta = reservedDelta,
            IdempotencyKey = idempotencyKey
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return CreditLedgerResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return await ResolveConflictAsync(walletId, idempotencyKey, cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await HasIdempotencyKeyAsync(walletId, idempotencyKey, cancellationToken))
            {
                return CreditLedgerResult.Failure("duplicate_idempotency_key");
            }

            throw;
        }
    }

    private async Task<CreditLedgerResult> ResolveConflictAsync(
        Guid walletId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await HasIdempotencyKeyAsync(walletId, idempotencyKey, cancellationToken)
            ? CreditLedgerResult.Failure("duplicate_idempotency_key")
            : CreditLedgerResult.Failure("concurrency_conflict");
    }

    private Task<bool> HasIdempotencyKeyAsync(
        Guid walletId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return db.CreditTransactions.AnyAsync(
            transaction => transaction.WalletId == walletId
                && transaction.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }
}
