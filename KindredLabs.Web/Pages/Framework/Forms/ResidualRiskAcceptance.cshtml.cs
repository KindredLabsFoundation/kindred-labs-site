using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Forms;

[Authorize]
public class ResidualRiskAcceptanceModel : PageModel
{
    private readonly IDraftService _draftService;
    private readonly IEncryptionService _encryptionService;
    private readonly ISubmissionService _submissionService;
    private readonly IPdfService _pdfService;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<Resources.Pages.Forms.ResidualRiskAcceptance> _localizer;

    public ResidualRiskAcceptanceModel(
        IDraftService draftService,
        IEncryptionService encryptionService,
        ISubmissionService submissionService,
        IPdfService pdfService,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<Resources.Pages.Forms.ResidualRiskAcceptance> localizer
    )
    {
        _draftService = draftService;
        _encryptionService = encryptionService;
        _submissionService = submissionService;
        _pdfService = pdfService;
        _emailService = emailService;
        _userManager = userManager;
        _localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsPreview { get; set; }
    public string? LastSaved { get; set; }
    public Guid DraftId { get; set; }

    public class InputModel
    {
        [Required]
        public string RecordId { get; set; } =
            $"{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8)}";

        public string? InternalReference { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public string VendorName { get; set; } = string.Empty;

        [Required]
        public string ModelNameAndVersion { get; set; } = string.Empty;

        public string PreparedByName { get; set; } = string.Empty;

        [Required]
        public string PreparedByRole { get; set; } = string.Empty;

        public DateTime? PreparedByDate { get; set; }

        public string? ReviewedByName { get; set; }
        public string? ReviewedByRole { get; set; }
        public DateTime? ReviewedByDate { get; set; }

        public List<ComplianceItem> DisclosureRequirements { get; set; } = new();
        public List<ComplianceItem> OngoingTransparency { get; set; } = new();
        public List<ComplianceItem> ContractualAccountability { get; set; } = new();
        public List<ComplianceItem> EvaluationCriteria { get; set; } = new();

        public void InitializeItems()
        {
            if (DisclosureRequirements.Count == 0)
            {
                DisclosureRequirements.AddRange(
                    new[]
                    {
                        new ComplianceItem { Id = "1.1", LabelKey = "Req1_1" },
                        new ComplianceItem { Id = "1.2", LabelKey = "Req1_2" },
                        new ComplianceItem { Id = "1.3", LabelKey = "Req1_3" },
                        new ComplianceItem { Id = "1.4", LabelKey = "Req1_4" },
                        new ComplianceItem { Id = "1.5", LabelKey = "Req1_5" },
                    }
                );
            }
            if (OngoingTransparency.Count == 0)
            {
                OngoingTransparency.AddRange(
                    new[]
                    {
                        new ComplianceItem { Id = "2.1", LabelKey = "Req2_1" },
                        new ComplianceItem { Id = "2.2", LabelKey = "Req2_2" },
                        new ComplianceItem { Id = "2.3", LabelKey = "Req2_3" },
                        new ComplianceItem { Id = "2.4", LabelKey = "Req2_4" },
                    }
                );
            }
            if (ContractualAccountability.Count == 0)
            {
                ContractualAccountability.AddRange(
                    new[]
                    {
                        new ComplianceItem { Id = "3.1", LabelKey = "Req3_1" },
                        new ComplianceItem { Id = "3.2", LabelKey = "Req3_2" },
                        new ComplianceItem { Id = "3.3", LabelKey = "Req3_3" },
                        new ComplianceItem { Id = "3.4", LabelKey = "Req3_4" },
                        new ComplianceItem { Id = "3.5", LabelKey = "Req3_5" },
                    }
                );
            }
            if (EvaluationCriteria.Count == 0)
            {
                EvaluationCriteria.AddRange(
                    new[]
                    {
                        new ComplianceItem
                        {
                            Id = "4.1",
                            LabelKey = "Req4_1",
                            UseMetNotMet = true,
                        },
                        new ComplianceItem
                        {
                            Id = "4.2",
                            LabelKey = "Req4_2",
                            UseMetNotMet = true,
                        },
                        new ComplianceItem
                        {
                            Id = "4.3",
                            LabelKey = "Req4_3",
                            UseMetNotMet = true,
                        },
                        new ComplianceItem
                        {
                            Id = "4.4",
                            LabelKey = "Req4_4",
                            UseMetNotMet = true,
                        },
                    }
                );
            }
        }

        // Section 2 & 3
        public string? GovernanceRiskAssessment { get; set; }
        public string? CompensatingControls { get; set; }

        // Section 4
        [Required]
        public string Decision { get; set; } = string.Empty;

        [Required]
        public string Rationale { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime? NextReviewDate { get; set; }

        public string? SignatureData { get; set; }
    }

    public class ComplianceItem
    {
        public string Id { get; set; } = string.Empty;
        public string LabelKey { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool UseMetNotMet { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.EmailConfirmed)
            return RedirectToPage("/Account/Login");

        Input.PreparedByName = user.UserName ?? user.Email ?? "User";

        var draft = await _draftService.GetDraftAsync(user.Id, FormType.ResidualRiskAcceptance);
        if (draft != null)
        {
            DraftId = draft.Id;
            var data = JsonSerializer.Deserialize<InputModel>(draft.FormData);
            if (data != null)
            {
                Input = data;
                Input.PreparedByName = user.UserName ?? user.Email ?? "User"; // Ensure it's up to date
                LastSaved = draft.LastSavedAt.ToString("g");
            }
        }
        Input.InitializeItems();

        return Page();
    }

    public async Task<IActionResult> OnPostSaveDraftAsync([FromBody] InputModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var json = JsonSerializer.Serialize(model);
        await _draftService.SaveDraftAsync(user.Id, FormType.ResidualRiskAcceptance, json);

        return new JsonResult(new { success = true, lastSaved = DateTime.Now.ToString("g") });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string? draftId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        await _draftService.DeleteDraftAsync(user.Id, FormType.ResidualRiskAcceptance);
        return RedirectToPage(
            "/Framework/Index",
            new
            {
                culture = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            }
        );
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.EmailConfirmed)
            return RedirectToPage("/Account/Login");

        if (action == "preview")
        {
            IsPreview = true;
            Input.InitializeItems();
            return Page();
        }

        if (!ModelState.IsValid)
        {
            Input.InitializeItems();
            return Page();
        }

        // Final Submission
        var submissionId = Guid.Parse(Input.RecordId.Split('-').Last());
        var formDataJson = JsonSerializer.Serialize(Input);

        var log = await _submissionService.LogSubmissionAsync(
            FormType.ResidualRiskAcceptance,
            formDataJson
        );

        var pdfBytes = await _pdfService.GenerateSubmissionPdfAsync(
            FormType.ResidualRiskAcceptance,
            formDataJson,
            submissionId,
            log.ContentHash,
            user.Email!,
            log.SubmittedAt,
            Input.SignatureData ?? string.Empty
        );

        await _emailService.SendFormSubmissionAsync(
            user.Email!,
            submissionId,
            FormType.ResidualRiskAcceptance,
            pdfBytes
        );

        await _draftService.DeleteDraftAsync(user.Id, FormType.ResidualRiskAcceptance);

        return File(pdfBytes, "application/pdf", $"ResidualRiskAcceptance_{Input.RecordId}.pdf");
    }
}
