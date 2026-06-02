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
public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISecurityService _securityService;
    private readonly ILogger<LoginWith2faModel> _logger;

    public LoginWith2faModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ISecurityService securityService,
        ILogger<LoginWith2faModel> logger,
        IStringLocalizer<Resources.Pages.Account.LoginWith2fa> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _securityService = securityService;
        _logger = logger;
        Localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    public IStringLocalizer<Resources.Pages.Account.LoginWith2fa> Localizer { get; }

    public class InputModel
    {
        [Required]
        [StringLength(7, MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string TwoFactorCode { get; set; } = string.Empty;

        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            return RedirectToPage("./Login", new { culture = GetCurrentCulture() });
        }

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
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

        var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        if (result.Succeeded)
        {
            _logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", user.Id);
            await _securityService.RecordLoginAttemptAsync(user.Id, true, ipAddress, userAgent);
            return LocalRedirect(returnUrl);
        }
        
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
            return RedirectToPage("./Lockout", new { culture = GetCurrentCulture() });
        }
        
        _logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", user.Id);
        await _securityService.RecordLoginAttemptAsync(user.Id, false, ipAddress, userAgent);
        ModelState.AddModelError(string.Empty, Localizer["InvalidAuthenticatorCode"]);
        return Page();
    }

    private string GetCurrentCulture()
    {
        return HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "en";
    }
}
