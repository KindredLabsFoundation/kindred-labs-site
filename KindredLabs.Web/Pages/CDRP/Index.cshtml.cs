using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.CDRP;

/// <summary>
/// Page model for the CDRP public information and expression of interest page.
/// </summary>
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICdrpCandidateService _candidateService;
    private readonly IEmailService _emailService;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<Resources.Pages.CDRP.Index> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    public IndexModel(
        UserManager<ApplicationUser> userManager,
        ICdrpCandidateService candidateService,
        IEmailService emailService,
        IEncryptionService encryptionService,
        IConfiguration configuration,
        IStringLocalizer<Resources.Pages.CDRP.Index> localizer
    )
    {
        _userManager = userManager;
        _candidateService = candidateService;
        _emailService = emailService;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _localizer = localizer;
    }

    /// <summary>
    /// Gets the string localizer for this page.
    /// </summary>
    public IStringLocalizer<Resources.Pages.CDRP.Index> Localizer => _localizer;

    /// <summary>
    /// Gets or sets the form input model.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether the Privacy Policy is already accepted by the user.
    /// </summary>
    public bool PrivacyPolicyAlreadyAccepted { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the Terms of Service are already accepted by the user.
    /// </summary>
    public bool TermsOfServiceAlreadyAccepted { get; private set; }

    /// <summary>
    /// Represents the form input fields for CDRP expression of interest.
    /// </summary>
    public class InputModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [EmailAddress]
        public string? ConfirmEmail { get; set; }

        public string? Organization { get; set; }

        public string? JobTitle { get; set; }

        [Required]
        public string Perspectives { get; set; } = string.Empty;

        [Required]
        public string Background { get; set; } = string.Empty;

        [Required]
        public string Interest { get; set; } = string.Empty;

        [Required]
        public string ReferralSource { get; set; } = string.Empty;

        public string? Conflicts { get; set; }

        [Required]
        [Range(typeof(bool), "true", "true")]
        public bool Acknowledgment { get; set; }

        [Required]
        [Range(
            typeof(bool),
            "true",
            "true",
            ErrorMessage = "You must agree to the Privacy Policy."
        )]
        public bool AcceptedPrivacyPolicy { get; set; }

        [Required]
        [Range(
            typeof(bool),
            "true",
            "true",
            ErrorMessage = "You must agree to the Terms of Service."
        )]
        public bool AcceptedTermsOfService { get; set; }
    }

    /// <summary>
    /// Handles GET requests to the CDRP page.
    /// </summary>
    public async Task OnGetAsync()
    {
        ViewData["Title"] = _localizer["PageTitle"];
        ViewData["Description"] = _localizer["PageDescription"];
        ViewData["CanonicalUrl"] = _localizer["CanonicalUrl"];
        ViewData["JsonLd"] = $@"{{
  ""@context"": ""https://schema.org"",
  ""@type"": ""WebPage"",
  ""name"": ""{ViewData["Title"]}"",
  ""url"": ""{ViewData["CanonicalUrl"]}""
}}";

        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            Input.Name = $"{user.FirstName} {user.LastName}".Trim();
            Input.Email = user.Email ?? string.Empty;
            Input.Organization = user.Organization;
            Input.JobTitle = user.JobTitle;

            // Check policy acceptance
            var currentPrivacyVersion = _configuration["PolicyVersions:PrivacyPolicy"];
            var currentTermsVersion = _configuration["PolicyVersions:TermsOfService"];

            PrivacyPolicyAlreadyAccepted = user.PrivacyPolicyVersion == currentPrivacyVersion;
            TermsOfServiceAlreadyAccepted = user.TermsOfServiceVersion == currentTermsVersion;

            if (PrivacyPolicyAlreadyAccepted)
            {
                Input.AcceptedPrivacyPolicy = true;
            }

            if (TermsOfServiceAlreadyAccepted)
            {
                Input.AcceptedTermsOfService = true;
            }
        }
    }

    /// <summary>
    /// Handles POST requests to submit the expression of interest.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        bool isPrefilled =
            user != null && Input.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase);

        if (!isPrefilled && string.IsNullOrEmpty(Input.ConfirmEmail))
        {
            ModelState.AddModelError("Input.ConfirmEmail", _localizer["ErrorConfirmEmailRequired"]);
        }

        if (
            !string.IsNullOrEmpty(Input.ConfirmEmail)
            && !Input.Email.Equals(Input.ConfirmEmail, StringComparison.OrdinalIgnoreCase)
        )
        {
            ModelState.AddModelError("Input.ConfirmEmail", _localizer["ErrorEmailMismatch"]);
        }

        if (!ModelState.IsValid)
        {
            var userOnFailure = await _userManager.GetUserAsync(User);
            if (userOnFailure != null)
            {
                var currentPrivacyVersion = _configuration["PolicyVersions:PrivacyPolicy"];
                var currentTermsVersion = _configuration["PolicyVersions:TermsOfService"];

                PrivacyPolicyAlreadyAccepted =
                    userOnFailure.PrivacyPolicyVersion == currentPrivacyVersion;
                TermsOfServiceAlreadyAccepted =
                    userOnFailure.TermsOfServiceVersion == currentTermsVersion;
            }
            return Page();
        }

        // Check for duplicate submission by email
        var candidates = (await _candidateService.GetAllCandidatesAsync()).ToList();

        // Existing duplicate check for non-denied records or denied > 6 months
        var existingCandidate = candidates.FirstOrDefault(c =>
            c.Email.Equals(Input.Email, StringComparison.OrdinalIgnoreCase)
        );

        if (existingCandidate != null)
        {
            if (existingCandidate.DeniedAt.HasValue)
            {
                var reapplyDate = existingCandidate.DeniedAt.Value.AddMonths(6);
                if (reapplyDate > DateTime.UtcNow)
                {
                    ModelState.AddModelError(
                        "Input.Email",
                        string.Format(
                            _localizer["ErrorDeniedRecently"],
                            reapplyDate.ToString("MMMM d, yyyy")
                        )
                    );
                    return Page();
                }
            }
            else
            {
                ModelState.AddModelError("Input.Email", _localizer["ErrorDuplicateEmail"]);
                return Page();
            }
        }

        var formDataJson = JsonSerializer.Serialize(Input);
        var encryptedFormData = _encryptionService.Encrypt(formDataJson);
        var userId = user?.Id;

        await _candidateService.SubmitExpressionOfInterestAsync(
            userId,
            Input.Email,
            encryptedFormData
        );

        // Update user policy acceptance if needed
        if (user != null)
        {
            var currentPrivacyVersion = _configuration["PolicyVersions:PrivacyPolicy"];
            var currentTermsVersion = _configuration["PolicyVersions:TermsOfService"];
            bool updated = false;

            if (user.PrivacyPolicyVersion != currentPrivacyVersion)
            {
                user.PrivacyPolicyVersion = currentPrivacyVersion;
                user.PrivacyPolicyAcceptedAt = DateTime.UtcNow;
                updated = true;
            }

            if (user.TermsOfServiceVersion != currentTermsVersion)
            {
                user.TermsOfServiceVersion = currentTermsVersion;
                user.TermsOfServiceAcceptedAt = DateTime.UtcNow;
                updated = true;
            }

            if (updated)
            {
                await _userManager.UpdateAsync(user);
            }
        }

        // Send confirmation email
        await _emailService.SendCdrpConfirmationAsync(Input.Email);

        TempData["SuccessMessage"] =
            _localizer["SuccessMessage"].Value + " " + _localizer["SuccessFollowUp"].Value;

        return RedirectToPage();
    }
}
