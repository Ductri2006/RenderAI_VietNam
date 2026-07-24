using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Tests.Domain;

public sealed class EnumContractTests
{
    [Fact]
    public void CreditTransactionTypeHasApprovedValues()
    {
        Assert.Equal(
            ["Grant", "Reserve", "Consume", "Refund", "Purchase"],
            Enum.GetNames<CreditTransactionType>());
    }

    [Fact]
    public void RenderJobStatusHasApprovedValues()
    {
        Assert.Equal(
            ["Queued", "Processing", "Succeeded", "Failed"],
            Enum.GetNames<RenderJobStatus>());
    }

    [Fact]
    public void SourceTypeHasApprovedValues()
    {
        Assert.Equal(["Upload", "Canvas"], Enum.GetNames<SourceType>());
    }
}
