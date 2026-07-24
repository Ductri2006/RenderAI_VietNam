namespace RenderVN.CoreApi.Domain;

public sealed class CreditWallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int AvailableCredits { get; set; }
    public int ReservedCredits { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<CreditTransaction> Transactions { get; set; } = [];
}
