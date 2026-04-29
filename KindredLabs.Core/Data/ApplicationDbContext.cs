using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Data;

/// <summary>
/// The database context for the Kindred Labs application, handling identity and core business entities.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    private readonly IDataProtectionProvider? _dataProtectionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the <see cref="DbContext"/>.</param>
    /// <param name="dataProtectionProvider">Optional provider for data protection services.</param>
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDataProtectionProvider? dataProtectionProvider = null
    )
        : base(options)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    /// <summary>
    /// Gets or sets the data protection keys for the application.
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    /// <summary>
    /// Gets or sets the form drafts created by users.
    /// </summary>
    public DbSet<Draft> Drafts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the submission logs for completed forms.
    /// </summary>
    public DbSet<SubmissionLog> SubmissionLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the candidates for the CDRP panel.
    /// </summary>
    public DbSet<CdrpCandidate> CdrpCandidates { get; set; } = null!;

    /// <summary>
    /// Gets or sets the comment periods for the framework versions.
    /// </summary>
    public DbSet<CommentPeriod> CommentPeriods { get; set; } = null!;

    /// <summary>
    /// Gets or sets the audit logs for administrative actions.
    /// </summary>
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; } = null!;

    /// <summary>
    /// Configures the schema needed for the identity framework and application entities.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Draft
        modelBuilder.Entity<Draft>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.FormType).HasConversion<string>();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // SubmissionLog
        modelBuilder.Entity<SubmissionLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FormType).HasConversion<string>();

            entity.HasIndex(e => e.SubmittedAt);
        });

        // CdrpCandidate
        modelBuilder.Entity<CdrpCandidate>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        // CommentPeriod
        modelBuilder.Entity<CommentPeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IsLocked);
        });

        // AdminAuditLog
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity
                .HasOne(e => e.PerformedByUser)
                .WithMany()
                .HasForeignKey(e => e.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(e => e.TargetUser)
                .WithMany()
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PerformedByUserId);
            entity.HasIndex(e => e.TargetUserId);
            entity.HasIndex(e => e.PerformedAt);
        });
    }
}
