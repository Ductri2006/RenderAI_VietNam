namespace RenderVN.CoreApi.Domain;

public sealed class RenderResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RenderJobId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public RenderJob RenderJob { get; set; } = null!;
}
