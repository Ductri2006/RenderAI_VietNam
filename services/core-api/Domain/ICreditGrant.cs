namespace RenderVN.CoreApi.Domain;

public interface ICreditGrant
{
    Task<CreditLedgerResult> GrantAsync(
        Guid walletId,
        int credits,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
