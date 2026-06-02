using System.ComponentModel.DataAnnotations;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Account;

[AllowAnonymous]
public class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISecurityService _securityService;
    private readonly ILogger<LoginWithRecoveryCodeModel> _logger;

    public LoginWithRecoveryCodeModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ISecurityService securityService,
        ILogger<LoginWithRecoveryCodeModel> logger,
        IStringLocalizer<Resources.Pages.Account.LoginWithRecoveryCode> localizer
    )
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _securityService = securityService;
        _logger = logger;
        Localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IStringLocalizer<Resources.Pages.Account.LoginWithRecoveryCode> Localizer { get; }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Text)]
        public string RecoveryCode { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToPage("./Login", new { culture = GetCurrentCulture() });
        }

        ReturnUrl = returnUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        returnUrl ??= Url.Content("~/");

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            return RedirectToPage("./Login", new { culture = GetCurrentCulture() });
        }

        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty).ToUpperInvariant();

        var storedCodes = await _userManager.GetValidTwoFactorProvidersAsync(user);
        _logger.LogInformation(
            "Attempting recovery code sign in for user {UserId} with code: {Code}",
            user.Id,
            recoveryCode
        );
        _logger.LogInformation("Valid 2FA providers: {Providers}", string.Join(", ", storedCodes));

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        _logger.LogInformation(
            "Recovery code sign in result - Succeeded: {Succeeded}, IsLockedOut: {LockedOut}, IsNotAllowed: {NotAllowed}",
            result.Succeeded,
            result.IsLockedOut,
            result.IsNotAllowed
        );

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "User with ID '{UserId}' logged in with a recovery code.",
                user.Id
            );
            await _securityService.RecordLoginAttemptAsync(user.Id, true, ipAddress, userAgent);
            await _securityService.RecordAuditEventAsync(
                user.Id,
                SecurityEventTypes.RecoveryCodeUsed,
                "User logged in with recovery code",
                ipAddress
            );
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
            return RedirectToPage("./Lockout", new { culture = GetCurrentCulture() });
        }

        _logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}'.", user.Id);
        await _securityService.RecordLoginAttemptAsync(user.Id, false, ipAddress, userAgent);
        ModelState.AddModelError(string.Empty, Localizer["InvalidRecoveryCode"]);
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
