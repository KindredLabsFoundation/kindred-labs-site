using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KindredLabs.Core.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Data Protection keys table
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    // Application tables
    public DbSet<Draft> Drafts { get; set; } = null!;
    public DbSet<SubmissionLog> SubmissionLogs { get; set; } = null!;
    public DbSet<CdrpCandidate> CdrpCandidates { get; set; } = null!;
    public DbSet<CommentPeriod> CommentPeriods { get; set; } = null!;
}
