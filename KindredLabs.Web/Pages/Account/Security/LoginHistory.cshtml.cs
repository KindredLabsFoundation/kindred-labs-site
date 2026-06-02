using System.Text;
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
public class LoginHistoryModel : PageModel
{
    private readonly ISecurityService _securityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<Resources.Pages.Account.Security.LoginHistory> _localizer;

    public LoginHistoryModel(
        ISecurityService securityService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<Resources.Pages.Account.Security.LoginHistory> localizer
    )
    {
        _securityService = securityService;
        _userManager = userManager;
        _localizer = localizer;
    }

    public class ParsedLoginHistory
    {
        public DateTime LoginAt { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string ParsedUserAgent { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public IEnumerable<ParsedLoginHistory> LoginHistory { get; set; } =
        Enumerable.Empty<ParsedLoginHistory>();
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
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        var allHistory = await _securityService.GetLoginHistoryAsync(user.Id, 100);
        var pageSize = 25;
        var totalRecords = allHistory.Count();
        TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        CurrentPage = p < 1 ? 1 : (p > TotalPages && TotalPages > 0 ? TotalPages : p);

        LoginHistory = allHistory
            .Skip((CurrentPage - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new ParsedLoginHistory
            {
                LoginAt = h.LoginAt,
                IpAddress = MapIpAddress(h.IpAddress),
                UserAgent = h.UserAgent ?? string.Empty,
                ParsedUserAgent = ParseUserAgent(h.UserAgent),
                Success = h.Success,
            });

        return Page();
    }

    private string MapIpAddress(string? ip)
    {
        if (string.IsNullOrEmpty(ip))
            return "—";
        if (ip == "::1" || ip == "127.0.0.1")
            return "localhost";
        return ip;
    }

    private string ParseUserAgent(string? ua)
    {
        if (string.IsNullOrEmpty(ua))
            return "—";

        string browser = "Unknown";
        if (ua.Contains("Chrome") && !ua.Contains("Edg"))
            browser = "Chrome";
        else if (ua.Contains("Edg"))
            browser = "Edge";
        else if (ua.Contains("Firefox"))
            browser = "Firefox";
        else if (ua.Contains("Safari") && !ua.Contains("Chrome"))
            browser = "Safari";
        else
            browser = ua.Length > 50 ? ua.Substring(0, 50) : ua;

        string os = "Unknown";
        if (ua.Contains("Windows"))
            os = "Windows";
        else if (ua.Contains("Mac"))
            os = "macOS";
        else if (ua.Contains("Linux"))
            os = "Linux";
        else if (ua.Contains("Android"))
            os = "Android";
        else if (ua.Contains("iPhone") || ua.Contains("iPad"))
            os = "iOS";

        if (browser == ua || browser == "Unknown" || browser == "—" || os == "Unknown")
            return browser;

        return $"{browser} on {os}";
    }

    public async Task<IActionResult> OnGetDownloadAsync(string format)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        string content;
        string contentType;
        string extension;

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            content = await _securityService.ExportLoginHistoryAsCsvAsync(user.Id);
            contentType = "text/csv";
            extension = "csv";
        }
        else
        {
            content = await _securityService.ExportLoginHistoryAsTxtAsync(user.Id);
            contentType = "text/plain";
            extension = "txt";
        }

        var bytes = Encoding.UTF8.GetBytes(content);
        var fileName = $"kindredlabs_login_history_{DateTime.UtcNow:yyyyMMdd}.{extension}";

        return File(bytes, contentType, fileName);
    }

    public IStringLocalizer<Resources.Pages.Account.Security.LoginHistory> Localizer => _localizer;
}
