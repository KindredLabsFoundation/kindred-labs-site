namespace KindredLabs.Core.Models.CDRP;

public class CommentPeriod
{
    public Guid Id { get; set; }
    public string FrameworkVersion { get; set; } = null!;
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public bool IsLocked { get; set; }
}
