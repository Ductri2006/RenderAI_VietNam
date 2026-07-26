using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RenderVN.CoreApi.Data;

namespace RenderVN.CoreApi.Domain;

public sealed class CreditLedger(AppDbContext db) : ICreditGrant
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
        var transaction = new CreditTransaction
        {
            WalletId = walletId,
            Type = type,
            AvailableDelta = availableDelta,
            ReservedDelta = reservedDelta,
            IdempotencyKey = idempotencyKey
        };
        db.CreditTransactions.Add(transaction);
        var suspendedEntries = SuspendUnrelatedChanges(wallet, transaction);

        try
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return CreditLedgerResult.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                await ResetOperationEntriesAsync(wallet, transaction, cancellationToken);
                return await ResolveConflictAsync(walletId, idempotencyKey, cancellationToken);
            }
            catch (DbUpdateException)
            {
                await ResetOperationEntriesAsync(wallet, transaction, cancellationToken);
                if (await HasIdempotencyKeyAsync(walletId, idempotencyKey, cancellationToken))
                {
                    return CreditLedgerResult.Failure("duplicate_idempotency_key");
                }

                throw;
            }
        }
        finally
        {
            RestoreSuspendedChanges(suspendedEntries);
        }
    }

    private List<SuspendedEntry> SuspendUnrelatedChanges(
        CreditWallet wallet,
        CreditTransaction transaction)
    {
        db.ChangeTracker.DetectChanges();
        var operationEntities = new HashSet<object>(
            [wallet, transaction],
            ReferenceEqualityComparer.Instance);
        var suspendedEntries = db.ChangeTracker.Entries()
            .Where(entry => !operationEntities.Contains(entry.Entity))
            .Where(entry => entry.State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
            .Where(entry => entry.Entity is not CreditTransaction
                || entry.State == EntityState.Added)
            .Select(SuspendedEntry.Capture)
            .ToList();

        foreach (var suspendedEntry in suspendedEntries)
        {
            db.Entry(suspendedEntry.Entity).State = EntityState.Detached;
        }

        return suspendedEntries;
    }

    private void RestoreSuspendedChanges(IEnumerable<SuspendedEntry> suspendedEntries)
    {
        foreach (var suspendedEntry in suspendedEntries)
        {
            var entry = db.Entry(suspendedEntry.Entity);
            entry.State = EntityState.Unchanged;
            foreach (var originalValue in suspendedEntry.OriginalValues)
            {
                entry.Property(originalValue.Key).OriginalValue = originalValue.Value;
            }

            if (suspendedEntry.State == EntityState.Modified)
            {
                foreach (var propertyName in suspendedEntry.ModifiedProperties)
                {
                    entry.Property(propertyName).IsModified = true;
                }
            }
            else
            {
                entry.State = suspendedEntry.State;
            }
        }
    }

    private sealed record SuspendedEntry(
        object Entity,
        EntityState State,
        IReadOnlyDictionary<string, object?> OriginalValues,
        IReadOnlyList<string> ModifiedProperties)
    {
        public static SuspendedEntry Capture(EntityEntry entry)
        {
            var originalValues = entry.State is EntityState.Modified or EntityState.Deleted
                ? entry.Properties.ToDictionary(
                    property => property.Metadata.Name,
                    property => property.OriginalValue)
                : new Dictionary<string, object?>();
            var modifiedProperties = entry.State == EntityState.Modified
                ? entry.Properties
                    .Where(property => property.IsModified)
                    .Select(property => property.Metadata.Name)
                    .ToArray()
                : [];
            return new SuspendedEntry(
                entry.Entity,
                entry.State,
                originalValues,
                modifiedProperties);
        }
    }

    private async Task ResetOperationEntriesAsync(
        CreditWallet wallet,
        CreditTransaction transaction,
        CancellationToken cancellationToken)
    {
        db.Entry(transaction).State = EntityState.Detached;
        await db.Entry(wallet).ReloadAsync(cancellationToken);
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
