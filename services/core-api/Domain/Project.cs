namespace RenderVN.CoreApi.Domain;

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<SourceImage> SourceImages { get; set; } = [];
    public ICollection<RenderJob> RenderJobs { get; set; } = [];
}
