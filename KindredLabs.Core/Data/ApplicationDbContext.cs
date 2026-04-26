using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    private readonly IDataProtectionProvider? _dataProtectionProvider;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDataProtectionProvider? dataProtectionProvider = null
    )
        : base(options)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<Draft> Drafts { get; set; } = null!;
    public DbSet<SubmissionLog> SubmissionLogs { get; set; } = null!;
    public DbSet<CdrpCandidate> CdrpCandidates { get; set; } = null!;
    public DbSet<CommentPeriod> CommentPeriods { get; set; } = null!;

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
    }
}
