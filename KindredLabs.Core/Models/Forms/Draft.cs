using KindredLabs.Core.Models.Identity;

namespace KindredLabs.Core.Models.Forms;

public enum FormType
{
    DataProvenance,
    ConsentDocumentation,
    MaturityAssessment,
    ResidualRiskAcceptance,
}

public class Draft
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public FormType FormType { get; set; }
    public string FormData { get; set; } = null!; // Encrypted JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
