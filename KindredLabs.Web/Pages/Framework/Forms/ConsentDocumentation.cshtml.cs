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
public class ConsentDocumentationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDraftService _draftService;
    private readonly IPdfService _pdfService;
    private readonly ISubmissionService _submissionService;
    private readonly IEmailService _emailService;
    private readonly IStringLocalizer<ConsentDocumentation> _localizer;

    public ConsentDocumentationModel(
        UserManager<ApplicationUser> userManager,
        IDraftService draftService,
        IPdfService pdfService,
        ISubmissionService submissionService,
        IEmailService emailService,
        IStringLocalizer<ConsentDocumentation> localizer
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
    public ConsentDocumentationForm Form { get; set; } = new();

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

        var draft = await _draftService.GetDraftAsync(user.Id, FormType.ConsentDocumentation);
        if (draft != null)
        {
            DraftId = draft.Id;
            try
            {
                Form =
                    JsonSerializer.Deserialize<ConsentDocumentationForm>(draft.FormData) ?? new();
                LastSavedAt = draft.LastSavedAt;
            }
            catch
            {
                Form = new ConsentDocumentationForm();
            }
        }
        else
        {
            Form = new ConsentDocumentationForm
            {
                RecordId =
                    $"{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}",
                PreparedByName = user.UserName ?? string.Empty,
                Date = DateTime.Today,
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveDraftAsync([FromBody] ConsentDocumentationForm form)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var json = JsonSerializer.Serialize(form);
        var draft = await _draftService.SaveDraftAsync(
            user.Id,
            FormType.ConsentDocumentation,
            json
        );

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

        var submission = await _submissionService.LogSubmissionAsync(
            FormType.ConsentDocumentation,
            formDataJson
        );

        var pdf = await _pdfService.GenerateSubmissionPdfAsync(
            FormType.ConsentDocumentation,
            formDataJson,
            submission.Id,
            submission.ContentHash,
            user.Email!,
            submission.SubmittedAt,
            signatureBase64
        );

        await _emailService.SendFormSubmissionAsync(
            user.Email!,
            submission.Id,
            FormType.ConsentDocumentation,
            pdf
        );

        await _draftService.DeleteDraftAsync(user.Id, FormType.ConsentDocumentation);

        return File(pdf, "application/pdf", $"ConsentDocumentation_{submission.Id}.pdf");
    }

    public async Task<IActionResult> OnPostDeleteAsync(string? draftId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        await _draftService.DeleteDraftAsync(user.Id, FormType.ConsentDocumentation);

        return RedirectToPage(
            "/Framework/Index",
            new { culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name }
        );
    }

    public class ConsentDocumentationForm
    {
        // Header
        [Required]
        public string RecordId { get; set; } = string.Empty;

        public string? InternalReference { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public string DataProvenanceRecordId { get; set; } = string.Empty;

        [Required]
        public string ModelNameAndVersion { get; set; } = string.Empty;

        public string PreparedByName { get; set; } = string.Empty;

        [Required]
        public string PreparedByRole { get; set; } = string.Empty;

        public string? ReviewedByName { get; set; }
        public string? ReviewedByRole { get; set; }
        public DateTime? ReviewDate { get; set; }

        // Section 1
        [Required]
        public string ConsentBasis { get; set; } = string.Empty; // Options: Explicit consent, Informed general consent, Implied organizational consent, No documented consent basis

        [Required]
        public string ClassificationRationale { get; set; } = string.Empty;

        // Section 2
        public string? InstrumentEstablishingConsent { get; set; }
        public string? DocumentLocation { get; set; }
        public string? RelevantProvision { get; set; }
        public bool NoInstrumentExists { get; set; }

        // Section 3
        [Required]
        public string SpecificFineTuningUse { get; set; } = string.Empty;

        [Required]
        public string ScopeAssessment { get; set; } = string.Empty; // Options: Yes, No, Uncertain

        [Required]
        public string ScopeAssessmentRationale { get; set; } = string.Empty;

        public string? ScopeGap { get; set; }

        // Section 4 - Elevated Review
        public string? ElevatedReviewAuthorityName { get; set; }
        public string? ElevatedReviewAuthorityRole { get; set; }
        public DateTime? ElevatedReviewDate { get; set; }
        public bool RoutedFromSection1 { get; set; }
        public bool RoutedFromSection2 { get; set; }
        public bool RoutedFromSection3 { get; set; }
        public string ElevatedReviewDecision { get; set; } = string.Empty; // Approved for Use, Approved with Conditions, Not Approved
        public string? DecisionRationale { get; set; }
        public string? ConditionsOnApproval { get; set; }

        // Section 5
        [Required]
        public string WithdrawalProcess { get; set; } = string.Empty;

        [Required]
        public string RemovalFromCorpus { get; set; } = string.Empty;

        [Required]
        public string ObligationForAlreadyTrainedModels { get; set; } = string.Empty;

        [Required]
        public string DeletionRequestProcess { get; set; } = string.Empty;
    }
}
