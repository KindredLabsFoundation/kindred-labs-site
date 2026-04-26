using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.CDRP;

public enum CandidateStatus
{
    Received,
    UnderReview,
    PendingContact,
}

public class CdrpCandidate
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public string FormData { get; set; } = null!; // Encrypted JSON
    public CandidateStatus Status { get; set; } = CandidateStatus.Received;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime StatusUpdatedAt { get; set; } = DateTime.UtcNow;
    public string? AdminNotes { get; set; }
}
