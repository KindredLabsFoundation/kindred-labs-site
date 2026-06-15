using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using KindredLabs.Core.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Account
{
    /// <summary>
    /// Page model for reviewing and accepting updated policies after login.
    /// </summary>
    [Authorize]
    public class ReviewPoliciesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer<Resources.Pages.ReviewPolicies> _localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReviewPoliciesModel"/> class.
        /// </summary>
        public ReviewPoliciesModel(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IStringLocalizer<Resources.Pages.ReviewPolicies> localizer)
        {
            _userManager = userManager;
            _configuration = configuration;
            _localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the return URL after successful policy acceptance.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets the policy acceptance input.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new();

        /// <summary>
        /// Gets a value indicating whether the Privacy Policy requires re-acceptance.
        /// </summary>
        public bool RequiresPrivacyPolicy { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the Terms of Service require re-acceptance.
        /// </summary>
        public bool RequiresTermsOfService { get; private set; }

        /// <summary>
        /// Input model for policy acceptance.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets a value indicating whether the Privacy Policy was accepted.
            /// </summary>
            public bool AcceptedPrivacyPolicy { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the Terms of Service were accepted.
            /// </summary>
            public bool AcceptedTermsOfService { get; set; }
        }

        /// <summary>
        /// Handles GET requests for the policy review page.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var currentPrivacyVersion = _configuration["PolicyVersions:PrivacyPolicy"];
            var currentTermsVersion = _configuration["PolicyVersions:TermsOfService"];

            RequiresPrivacyPolicy = user.PrivacyPolicyVersion != currentPrivacyVersion;
            RequiresTermsOfService = user.TermsOfServiceVersion != currentTermsVersion;

            if (!RequiresPrivacyPolicy && !RequiresTermsOfService)
            {
                return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
            }

            return Page();
        }

        /// <summary>
        /// Handles POST requests for policy acceptance.
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var currentPrivacyVersion = _configuration["PolicyVersions:PrivacyPolicy"];
            var currentTermsVersion = _configuration["PolicyVersions:TermsOfService"];

            RequiresPrivacyPolicy = user.PrivacyPolicyVersion != currentPrivacyVersion;
            RequiresTermsOfService = user.TermsOfServiceVersion != currentTermsVersion;

            bool isValid = true;

            if (RequiresPrivacyPolicy && !Input.AcceptedPrivacyPolicy)
            {
                ModelState.AddModelError("Input.AcceptedPrivacyPolicy", _localizer["ErrorPrivacyPolicyRequired"]);
                isValid = false;
            }

            if (RequiresTermsOfService && !Input.AcceptedTermsOfService)
            {
                ModelState.AddModelError("Input.AcceptedTermsOfService", _localizer["ErrorTermsOfServiceRequired"]);
                isValid = false;
            }

            if (!isValid)
            {
                return Page();
            }

            bool updated = false;
            if (RequiresPrivacyPolicy)
            {
                user.PrivacyPolicyVersion = currentPrivacyVersion;
                user.PrivacyPolicyAcceptedAt = DateTime.UtcNow;
                updated = true;
            }

            if (RequiresTermsOfService)
            {
                user.TermsOfServiceVersion = currentTermsVersion;
                user.TermsOfServiceAcceptedAt = DateTime.UtcNow;
                updated = true;
            }

            if (updated)
            {
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }

            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        /// <summary>
        /// Gets the localizer for the page.
        /// </summary>
        public IStringLocalizer<Resources.Pages.ReviewPolicies> Localizer => _localizer;
    }
}
