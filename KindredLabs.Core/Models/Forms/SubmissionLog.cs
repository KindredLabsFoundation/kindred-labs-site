namespace KindredLabs.Core.Models.Forms;

public class SubmissionLog
{
    public Guid Id { get; set; } // This is the Submission ID embedded in the PDF
    public FormType FormType { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string ContentHash { get; set; } = null!; // SHA-256 hash of submitted form data
}
