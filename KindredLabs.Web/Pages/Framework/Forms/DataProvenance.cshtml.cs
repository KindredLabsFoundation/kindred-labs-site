using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Resources.Pages.Forms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Forms;

[Authorize]
public class DataProvenanceModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDraftService _draftService;
    private readonly IPdfService _pdfService;
    private readonly ISubmissionService _submissionService;
    private readonly IEmailService _emailService;
    private readonly IStringLocalizer<DataProvenance> _localizer;

    public DataProvenanceModel(
        UserManager<ApplicationUser> userManager,
        IDraftService draftService,
        IPdfService pdfService,
        ISubmissionService submissionService,
        IEmailService emailService,
        IStringLocalizer<DataProvenance> localizer
    )
    {
        _userManager = userManager;
        _draftService = draftService;
        _pdfService = pdfService;
        _submissionService = submissionService;
        _emailService = emailService;
        _localizer = localizer;
    }

    [BindProperty]
    public DataProvenanceForm Form { get; set; } = new();

    public string? StatusMessage { get; set; }
    public DateTime? LastSavedAt { get; set; }
    public Guid DraftId { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.EmailConfirmed)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        // Try to load existing draft for this user and form type
        var draft = await _draftService.GetDraftAsync(user.Id, FormType.DataProvenance);
        if (draft != null)
        {
            DraftId = draft.Id;
            try
            {
                Form = JsonSerializer.Deserialize<DataProvenanceForm>(draft.FormData) ?? new();
                LastSavedAt = draft.LastSavedAt;
            }
            catch
            {
                // If deserialization fails, we just use a fresh form
                Form = new DataProvenanceForm();
            }
        }
        else
        {
            Form = new DataProvenanceForm
            {
                RecordId =
                    $"{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}",
                PreparedByName = user.UserName ?? string.Empty, // Or a better name property if available
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveDraftAsync([FromBody] DataProvenanceForm form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var json = JsonSerializer.Serialize(form);
        var draft = await _draftService.SaveDraftAsync(user.Id, FormType.DataProvenance, json);

        return new JsonResult(new { lastSavedAt = draft.LastSavedAt.ToString("g") });
    }

    public async Task<IActionResult> OnPostSubmitAsync(string signatureBase64)
    {
        if (!ModelState.IsValid)
            return Page();
        if (string.IsNullOrEmpty(signatureBase64))
        {
            ModelState.AddModelError("", _localizer["SignatureRequired"]);
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var formDataJson = JsonSerializer.Serialize(Form);

        // 1. Log submission (generates ID and hash)
        var submission = await _submissionService.LogSubmissionAsync(
            FormType.DataProvenance,
            formDataJson
        );

        // 2. Generate PDF
        var pdf = await _pdfService.GenerateSubmissionPdfAsync(
            FormType.DataProvenance,
            formDataJson,
            submission.Id,
            submission.ContentHash,
            user.Email!,
            submission.SubmittedAt,
            signatureBase64
        );

        // 3. Email PDF
        await _emailService.SendFormSubmissionAsync(
            user.Email!,
            submission.Id,
            FormType.DataProvenance,
            pdf
        );

        // 4. Purge draft
        await _draftService.DeleteDraftAsync(user.Id, FormType.DataProvenance);

        // 5. Return PDF for browser to open
        return File(pdf, "application/pdf", $"DataProvenance_{submission.Id}.pdf");
    }

    public async Task<IActionResult> OnPostDeleteAsync(string? draftId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        await _draftService.DeleteDraftAsync(user.Id, FormType.DataProvenance);

        return RedirectToPage(
            "/Framework/Index",
            new { culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name }
        );
    }

    public class DataProvenanceForm
    {
        // Header
        public string RecordId { get; set; } = string.Empty;
        public string? InternalReference { get; set; }
        public DateTime? Date { get; set; }
        public string ModelNameAndVersion { get; set; } = string.Empty;
        public string PreparedByName { get; set; } = string.Empty;
        public string PreparedByRole { get; set; } = string.Empty;
        public DateTime? PreparedByDate { get; set; }
        public string? ReviewedByName { get; set; }
        public string? ReviewedByRole { get; set; }
        public DateTime? ReviewedByDate { get; set; }

        // Section 1
        public string DataSourceName { get; set; } = string.Empty;
        public string GeneratingSystemOrProcess { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        public string PopulationReflected { get; set; } = string.Empty;
        public string? KnownBias { get; set; } // Yes, No, Unknown
        public string? BiasDescription { get; set; }

        // Section 2
        public string OriginalCollectionPurpose { get; set; } = string.Empty;
        public string? CompatibilityAssessment { get; set; } // Yes, No, Requires Elevated Review
        public string CompatibilityExplanation { get; set; } = string.Empty;
        public string? IndividualAwareness { get; set; } // Yes, No, Unknown

        // Section 3
        public string PopulationsRepresented { get; set; } = string.Empty;
        public string PopulationsUnderrepresented { get; set; } = string.Empty;
        public string? HistoricalPatterns { get; set; } // Yes, No, Unknown
        public string? PatternsDescription { get; set; }
        public string? AdequacyAssessment { get; set; } // Adequate, Requires Remediation, Requires Elevated Review
        public string? RemediationDescription { get; set; }

        // Section 4
        public string? KnownGaps { get; set; } // Yes, No
        public string? GapsDescription { get; set; }
        public string? ConditionsOfUnreliability { get; set; } // Yes, No
        public string? ConditionsDescription { get; set; }
        public string? QualityAssessment { get; set; }

        // Section 5
        public string? PriorUse { get; set; } // Yes, No
        public string? PriorUseDescription { get; set; }
        public string? CumulativeEffectAssessment { get; set; } // Yes, No, Unknown
        public string? CumulativeEffectDescription { get; set; }

        // Elevated Review
        public string? ElevatedReviewAuthorityName { get; set; }
        public string? ElevatedReviewAuthorityRole { get; set; }
        public bool RoutedFromSection2 { get; set; }
        public bool RoutedFromSection3 { get; set; }
        public string? ElevatedReviewDecision { get; set; } // Approved, Approved with Conditions, Not Approved
        public string? ElevatedReviewRationale { get; set; }
        public string? ConditionsOnApproval { get; set; }
    }
}
