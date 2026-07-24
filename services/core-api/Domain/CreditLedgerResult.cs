namespace RenderVN.CoreApi.Domain;

public sealed record CreditLedgerResult(bool IsSuccess, string? ErrorCode)
{
    public static CreditLedgerResult Success() => new(true, null);
    public static CreditLedgerResult Failure(string errorCode) => new(false, errorCode);
}
