namespace RenderVN.CoreApi.Domain;

public sealed class RenderJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid SourceImageId { get; set; }
    public Guid? StylePresetId { get; set; }
    public RenderJobStatus Status { get; set; } = RenderJobStatus.Pending;
    public int CreditCost { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public SourceImage SourceImage { get; set; } = null!;
    public StylePreset? StylePreset { get; set; }
    public RenderResult? Result { get; set; }
}
