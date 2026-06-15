// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Resources.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace KindredLabs.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISecurityService _securityService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginModel> _logger;
        private readonly IStringLocalizer<Login> _localizer;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ISecurityService securityService,
            IConfiguration configuration,
            ILogger<LoginModel> logger,
            IStringLocalizer<Login> localizer
        )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _securityService = securityService;
            _configuration = configuration;
            _logger = logger;
            _localizer = localizer;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (
                await _signInManager.GetExternalAuthenticationSchemesAsync()
            ).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (
                await _signInManager.GetExternalAuthenticationSchemesAsync()
            ).ToList();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user != null)
                {
                    // Check if the user is suspended BEFORE attempting to sign in
                    if (user.IsSuspended)
                    {
                        _logger.LogWarning(
                            "Suspended user account '{UserId}' attempted to log in.",
                            user.Id
                        );
                        ModelState.AddModelError(string.Empty, _localizer["AccountSuspended"]);
                        return Page();
                    }

                    // Check if the user is scheduled for deletion
                    if (user.IsDeleted)
                    {
                        _logger.LogWarning(
                            "Soft-deleted user account '{UserId}' attempted to log in.",
                            user.Id
                        );
                        ModelState.AddModelError(
                            string.Empty,
                            _localizer["AccountScheduledForDeletion"]
                        );
                        return Page();
                    }

                    // Check password manually since PasswordSignInAsync signs the user in
                    var isPasswordValid = await _userManager.CheckPasswordAsync(
                        user,
                        Input.Password
                    );

                    await _securityService.RecordLoginAttemptAsync(
                        user.Id,
                        isPasswordValid,
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Request.Headers["User-Agent"].ToString()
                    );

                    if (!isPasswordValid)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            _localizer["Invalid login attempt."]
                        );
                        return Page();
                    }
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    if (user != null && !user.EmailConfirmed)
                    {
                        _logger.LogWarning(
                            "User with ID '{UserId}' tried to log in with unconfirmed email.",
                            user.Id
                        );
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "EmailNotConfirmed");
                        return Page();
                    }

                    _logger.LogInformation("User logged in.");

                    // Check if policy re-acceptance is needed
                    var currentPrivacyVersion = _configuration["PolicyVersions:PrivacyPolicy"];
                    var currentTermsVersion = _configuration["PolicyVersions:TermsOfService"];

                    if (
                        user.PrivacyPolicyVersion != currentPrivacyVersion
                        || user.TermsOfServiceVersion != currentTermsVersion
                    )
                    {
                        var culture =
                            HttpContext
                                .Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>()
                                ?.RequestCulture.Culture.TwoLetterISOLanguageName
                            ?? "en";
                        return RedirectToPage(
                            "./ReviewPolicies",
                            new { ReturnUrl = returnUrl, culture = culture }
                        );
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    var culture =
                        HttpContext
                            .Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>()
                            ?.RequestCulture.Culture.TwoLetterISOLanguageName
                        ?? "en";

                    return RedirectToPage(
                        "./LoginWith2fa",
                        new
                        {
                            ReturnUrl = returnUrl,
                            RememberMe = Input.RememberMe,
                            culture = culture,
                        }
                    );
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    var culture =
                        HttpContext
                            .Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>()
                            ?.RequestCulture.Culture.TwoLetterISOLanguageName
                        ?? "en";
                    return RedirectToPage("./Lockout", new { culture = culture });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, _localizer["Invalid login attempt."]);
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
