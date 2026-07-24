using Microsoft.AspNetCore.Identity;

namespace RenderVN.CoreApi.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CreditWallet? CreditWallet { get; set; }
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<SourceImage> SourceImages { get; set; } = [];
    public ICollection<RenderJob> RenderJobs { get; set; } = [];
    public ICollection<RenderResult> RenderResults { get; set; } = [];
    public ICollection<PaymentOrder> PaymentOrders { get; set; } = [];
    public ICollection<AuditEvent> AuditEvents { get; set; } = [];
}
