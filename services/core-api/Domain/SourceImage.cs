namespace RenderVN.CoreApi.Domain;

public sealed class SourceImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public SourceType SourceType { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<RenderJob> RenderJobs { get; set; } = [];
}
