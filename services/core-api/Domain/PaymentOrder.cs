namespace RenderVN.CoreApi.Domain;

public sealed class PaymentOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int Credits { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Pending;
    public string? ProviderReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
