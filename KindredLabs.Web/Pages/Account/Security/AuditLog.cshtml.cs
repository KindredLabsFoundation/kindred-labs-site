using System.Globalization;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Account.Security;

[Authorize]
public class AuditLogModel : PageModel
{
    private readonly ISecurityService _securityService;
    private readonly UserManager<ApplicationUser> _userManager;
    public IStringLocalizer<Resources.Pages.Account.Security.AuditLog> Localizer { get; }

    public AuditLogModel(
        ISecurityService securityService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<Resources.Pages.Account.Security.AuditLog> localizer
    )
    {
        _securityService = securityService;
        _userManager = userManager;
        Localizer = localizer;
    }

    public class ParsedAuditLog
    {
        public DateTime PerformedAt { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string DisplayEvent { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public IEnumerable<ParsedAuditLog> AuditLog { get; set; } = Enumerable.Empty<ParsedAuditLog>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public string CurrentCulture =>
        HttpContext
            .Features.Get<IRequestCultureFeature>()
            ?.RequestCulture.Culture.TwoLetterISOLanguageName
        ?? "en";

    public async Task<IActionResult> OnGetAsync(int p = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        var allLogs = await _securityService.GetAuditLogAsync(user.Id, 100);
        int pageSize = 25;
        int totalRecords = allLogs.Count();
        TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        CurrentPage = p < 1 ? 1 : (p > TotalPages && TotalPages > 0 ? TotalPages : p);

        AuditLog = allLogs
            .Skip((CurrentPage - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ParsedAuditLog
            {
                PerformedAt = l.PerformedAt,
                EventType = l.EventType,
                DisplayEvent = MapEventType(l.EventType),
                Description = l.Description ?? "—",
                IpAddress = MapIpAddress(l.IpAddress),
            });

        return Page();
    }

    private string MapEventType(string eventType)
    {
        return eventType switch
        {
            SecurityEventTypes.PasswordChanged => Localizer["EventPasswordChanged"],
            SecurityEventTypes.PrimaryEmailChanged => Localizer["EventPrimaryEmailChanged"],
            SecurityEventTypes.EmailAdded => Localizer["EventEmailAdded"],
            SecurityEventTypes.EmailRemoved => Localizer["EventEmailRemoved"],
            SecurityEventTypes.TwoFactorEnabled => Localizer["EventTwoFactorEnabled"],
            SecurityEventTypes.TwoFactorDisabled => Localizer["EventTwoFactorDisabled"],
            SecurityEventTypes.AccountDeletionInitiated => Localizer[
                "EventAccountDeletionInitiated"
            ],
            SecurityEventTypes.OtherSessionsRevoked => Localizer["EventOtherSessionsRevoked"],
            _ => eventType,
        };
    }

    private string MapIpAddress(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
            return "—";
        if (ip == "::1" || ip == "127.0.0.1")
            return "localhost";
        return ip;
    }

    public async Task<IActionResult> OnGetDownloadAsync(string format)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        string content;
        string contentType;
        string extension;

        if (format?.ToLower() == "csv")
        {
            content = await _securityService.ExportAuditLogAsCsvAsync(user.Id);
            contentType = "text/csv";
            extension = "csv";
        }
        else
        {
            content = await _securityService.ExportAuditLogAsTxtAsync(user.Id);
            contentType = "text/plain";
            extension = "txt";
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        return File(bytes, contentType, $"kindredlabs_audit_log_{date}.{extension}");
    }
}
