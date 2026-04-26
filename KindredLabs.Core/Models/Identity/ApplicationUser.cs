using Microsoft.AspNetCore.Identity;

namespace KindredLabs.Core.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string? PreferredLocale { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
