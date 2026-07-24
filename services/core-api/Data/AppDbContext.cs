using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<CreditWallet> CreditWallets => Set<CreditWallet>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<SourceImage> SourceImages => Set<SourceImage>();
    public DbSet<RenderJob> RenderJobs => Set<RenderJob>();
    public DbSet<RenderResult> RenderResults => Set<RenderResult>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<StylePreset> StylePresets => Set<StylePreset>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureLedgerIsAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnsureLedgerIsAppendOnly();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void EnsureLedgerIsAppendOnly()
    {
        var mutatesLedger = ChangeTracker.Entries<CreditTransaction>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted);
        if (mutatesLedger)
        {
            throw new InvalidOperationException("Credit transactions are immutable.");
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();

        builder.Entity<CreditWallet>(entity =>
        {
            entity.HasKey(wallet => wallet.Id);
            entity.HasIndex(wallet => wallet.UserId).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_CreditWallet_Available_Nonnegative", "\"AvailableCredits\" >= 0");
                table.HasCheckConstraint("CK_CreditWallet_Reserved_Nonnegative", "\"ReservedCredits\" >= 0");
            });
            entity.HasOne(wallet => wallet.User)
                .WithOne(user => user.CreditWallet)
                .HasForeignKey<CreditWallet>(wallet => wallet.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.IdempotencyKey).HasMaxLength(200);
            entity.HasIndex(transaction => new { transaction.WalletId, transaction.CreatedAt });
            entity.HasIndex(transaction => new { transaction.WalletId, transaction.IdempotencyKey })
                .IsUnique();
            entity.HasOne(transaction => transaction.Wallet)
                .WithMany(wallet => wallet.Transactions)
                .HasForeignKey(transaction => transaction.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Project>(entity =>
        {
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Name).HasMaxLength(200);
            entity.HasIndex(project => project.UserId);
            entity.HasOne(project => project.User)
                .WithMany(user => user.Projects)
                .HasForeignKey(project => project.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SourceImage>(entity =>
        {
            entity.HasKey(image => image.Id);
            entity.Property(image => image.StorageKey).HasMaxLength(500);
            entity.Property(image => image.MimeType).HasMaxLength(100);
            entity.HasOne(image => image.User)
                .WithMany(user => user.SourceImages)
                .HasForeignKey(image => image.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(image => image.Project)
                .WithMany(project => project.SourceImages)
                .HasForeignKey(image => image.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RenderJob>(entity =>
        {
            entity.HasKey(job => job.Id);
            entity.HasIndex(job => new { job.UserId, job.CreatedAt });
            entity.HasOne(job => job.User)
                .WithMany(user => user.RenderJobs)
                .HasForeignKey(job => job.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(job => job.Project)
                .WithMany(project => project.RenderJobs)
                .HasForeignKey(job => job.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(job => job.SourceImage)
                .WithMany(image => image.RenderJobs)
                .HasForeignKey(job => job.SourceImageId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(job => job.StylePreset)
                .WithMany(preset => preset.RenderJobs)
                .HasForeignKey(job => job.StylePresetId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<RenderResult>(entity =>
        {
            entity.HasKey(result => result.Id);
            entity.Property(result => result.StorageKey).HasMaxLength(500);
            entity.HasIndex(result => result.RenderJobId).IsUnique();
            entity.HasOne(result => result.User)
                .WithMany(user => user.RenderResults)
                .HasForeignKey(result => result.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(result => result.RenderJob)
                .WithOne(job => job.Result)
                .HasForeignKey<RenderResult>(result => result.RenderJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PaymentOrder>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Amount).HasPrecision(18, 2);
            entity.Property(order => order.Currency).HasMaxLength(3);
            entity.Property(order => order.ProviderReference).HasMaxLength(200);
            entity.HasOne(order => order.User)
                .WithMany(user => user.PaymentOrders)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StylePreset>(entity =>
        {
            entity.HasKey(preset => preset.Id);
            entity.Property(preset => preset.Name).HasMaxLength(200);
            entity.Property(preset => preset.Slug).HasMaxLength(100);
            entity.HasIndex(preset => preset.Slug).IsUnique();
        });

        builder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(auditEvent => auditEvent.Id);
            entity.Property(auditEvent => auditEvent.EventType).HasMaxLength(200);
            entity.Property(auditEvent => auditEvent.EntityType).HasMaxLength(200);
            entity.HasOne(auditEvent => auditEvent.User)
                .WithMany(user => user.AuditEvents)
                .HasForeignKey(auditEvent => auditEvent.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
