using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Account.Manage;

[Authorize]
public class RecoveryCodesModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISecurityService _securityService;
    private readonly ILogger<RecoveryCodesModel> _logger;
    private readonly IStringLocalizer<Resources.Pages.Account.Manage.RecoveryCodes> _localizer;

    public IStringLocalizer<Resources.Pages.Account.Manage.RecoveryCodes> Localizer { get; }

    public RecoveryCodesModel(
        UserManager<ApplicationUser> userManager,
        ISecurityService securityService,
        ILogger<RecoveryCodesModel> logger,
        IStringLocalizer<Resources.Pages.Account.Manage.RecoveryCodes> localizer
    )
    {
        _userManager = userManager;
        _securityService = securityService;
        _logger = logger;
        _localizer = localizer;
        Localizer = localizer;
    }

    public int RecoveryCodeCount { get; set; }
    public IEnumerable<string>? GeneratedCodes { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { culture = GetCurrentCulture() });
        }

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (!isTwoFactorEnabled)
        {
            return RedirectToPage(
                "/Account/Manage/TwoFactor",
                new { culture = GetCurrentCulture() }
            );
        }

        RecoveryCodeCount = await _userManager.CountRecoveryCodesAsync(user);

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { culture = GetCurrentCulture() });
        }

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
        {
            _logger.LogError(
                "Cannot generate recovery codes - 2FA not enabled for user {UserId}",
                user.Id
            );
            ModelState.AddModelError(
                string.Empty,
                "Cannot generate recovery codes - 2FA not enabled."
            );
            RecoveryCodeCount = await _userManager.CountRecoveryCodesAsync(user);
            return Page();
        }

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        if (codes == null)
        {
            _logger.LogError("Failed to generate recovery codes for user {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Failed to generate recovery codes.");
            RecoveryCodeCount = await _userManager.CountRecoveryCodesAsync(user);
            return Page();
        }

        GeneratedCodes = codes.ToArray();
        _logger.LogInformation(
            "Generated first recovery code sample: {Code}",
            GeneratedCodes.First()
        );

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _securityService.RecordAuditEventAsync(
            user.Id,
            SecurityEventTypes.RecoveryCodesGenerated,
            null,
            ipAddress
        );

        RecoveryCodeCount = await _userManager.CountRecoveryCodesAsync(user);
        TempData["StatusMessage"] = _localizer["RecoveryCodesGeneratedSuccess"].Value;

        return Page();
    }

    private string GetCurrentCulture()
    {
        return HttpContext
                .Features.Get<IRequestCultureFeature>()
                ?.RequestCulture.Culture.TwoLetterISOLanguageName
            ?? "en";
    }
}
