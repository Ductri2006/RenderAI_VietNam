namespace RenderVN.CoreApi.Domain;

public enum PaymentOrderStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4,
    Refunded = 5
}
