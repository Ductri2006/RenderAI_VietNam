namespace RenderVN.CoreApi.Domain;

public sealed class CreditTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletId { get; set; }
    public CreditTransactionType Type { get; set; }
    public int AvailableDelta { get; set; }
    public int ReservedDelta { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CreditWallet Wallet { get; set; } = null!;
}
