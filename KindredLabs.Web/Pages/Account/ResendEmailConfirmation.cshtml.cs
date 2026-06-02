using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Resources.Pages.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Account;

[AllowAnonymous]
public class ResendEmailConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IStringLocalizer<ResendEmailConfirmation> _localizer;

    public ResendEmailConfirmationModel(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IStringLocalizer<ResendEmailConfirmation> localizer
    )
    {
        _userManager = userManager;
        _emailService = emailService;
        _localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                _localizer["Verification email sent. Please check your email."]
            );
            return Page();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var culture = RouteData.Values["culture"]?.ToString() ?? "en";
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new
            {
                userId = userId,
                code = code,
                culture = culture,
            },
            protocol: Request.Scheme
        );

        if (callbackUrl != null)
        {
            await _emailService.SendRegistrationConfirmationAsync(Input.Email, callbackUrl);
        }

        ModelState.AddModelError(
            string.Empty,
            _localizer["Verification email sent. Please check your email."]
        );
        return Page();
    }
}
