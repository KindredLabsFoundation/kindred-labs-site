using System.Globalization;
using System.Text;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using QRCoder;

namespace KindredLabs.Web.Pages.Account.Manage;

[Authorize]
public class TwoFactorModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ISecurityService _securityService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<Resources.Pages.Account.Manage.TwoFactor> _localizer;

    public IStringLocalizer<Resources.Pages.Account.Manage.TwoFactor> Localizer { get; }

    public TwoFactorModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ISecurityService securityService,
        IEmailService emailService,
        IConfiguration configuration,
        IStringLocalizer<Resources.Pages.Account.Manage.TwoFactor> localizer
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _securityService = securityService;
        _emailService = emailService;
        _configuration = configuration;
        _localizer = localizer;
        Localizer = localizer;
    }

    public bool TwoFactorEnabled { get; set; }
    public string? AuthenticatorKey { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? StatusMessage { get; set; }

    [BindProperty]
    public string? Code { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { culture = GetCurrentCulture() });
        }

        TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        if (!TwoFactorEnabled)
        {
            await LoadAuthenticatorKeyAsync(user);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostEnableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { culture = GetCurrentCulture() });
        }

        if (string.IsNullOrWhiteSpace(Code))
        {
            ModelState.AddModelError("Code", _localizer["VerificationCodeRequired"]);
            TwoFactorEnabled = false;
            await LoadAuthenticatorKeyAsync(user);
            return Page();
        }

        // Remove any spaces or dashes from the code
        var verificationCode = Code.Replace(" ", string.Empty).Replace("-", string.Empty);

        var isTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            verificationCode
        );

        if (!isTokenValid)
        {
            ModelState.AddModelError("Code", _localizer["InvalidVerificationCode"]);
            TwoFactorEnabled = false;
            await LoadAuthenticatorKeyAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _securityService.RecordAuditEventAsync(
            user.Id,
            SecurityEventTypes.TwoFactorEnabled,
            null,
            ipAddress
        );

        var securityEmail =
            _configuration["Contact:SecurityEmail"] ?? "security@kindredlabsfoundation.org";
        await _emailService.SendTwoFactorEnabledNotificationAsync(user.Email!, securityEmail);

        TempData["StatusMessage"] = _localizer["TwoFactorEnabledSuccess"].Value;
        return RedirectToPage(new { culture = GetCurrentCulture() });
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { culture = GetCurrentCulture() });
        }

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            TwoFactorEnabled = true;
            return Page();
        }

        await _userManager.ResetAuthenticatorKeyAsync(user);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _securityService.RecordAuditEventAsync(
            user.Id,
            SecurityEventTypes.TwoFactorDisabled,
            null,
            ipAddress
        );

        var securityEmail =
            _configuration["Contact:SecurityEmail"] ?? "security@kindredlabsfoundation.org";
        await _emailService.SendTwoFactorDisabledNotificationAsync(user.Email!, securityEmail);

        TempData["StatusMessage"] = _localizer["TwoFactorDisabledSuccess"].Value;
        return RedirectToPage(new { culture = GetCurrentCulture() });
    }

    private async Task LoadAuthenticatorKeyAsync(ApplicationUser user)
    {
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        AuthenticatorKey = FormatKey(unformattedKey!);
        QrCodeBase64 = GenerateQrCode(user.Email!, unformattedKey!);
    }

    private string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.Substring(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private string GenerateQrCode(string email, string unformattedKey)
    {
        var issuer = "Kindred Labs Foundation";
        var uri =
            $"otpauth://totp/{issuer}:{email}?secret={unformattedKey}&issuer={issuer}&digits=6";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(10);
        return Convert.ToBase64String(qrCodeImage);
    }

    private string GetCurrentCulture()
    {
        return HttpContext
                .Features.Get<IRequestCultureFeature>()
                ?.RequestCulture.Culture.TwoLetterISOLanguageName
            ?? "en";
    }
}
