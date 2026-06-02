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

namespace KindredLabs.Web.Pages.Framework.Forms;

[Authorize]
public class MaturityAssessmentModel : PageModel
{
    private readonly IDraftService _draftService;
    private readonly IEncryptionService _encryptionService;
    private readonly ISubmissionService _submissionService;
    private readonly IPdfService _pdfService;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<Resources.Pages.Forms.MaturityAssessment> _localizer;
    private readonly IConfiguration _configuration;

    public MaturityAssessmentModel(
        IDraftService draftService,
        IEncryptionService encryptionService,
        ISubmissionService submissionService,
        IPdfService pdfService,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<Resources.Pages.Forms.MaturityAssessment> localizer,
        IConfiguration configuration
    )
    {
        _draftService = draftService;
        _encryptionService = encryptionService;
        _submissionService = submissionService;
        _pdfService = pdfService;
        _emailService = emailService;
        _userManager = userManager;
        _localizer = localizer;
        _configuration = configuration;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? LastSaved { get; set; }
    public bool IsPreview { get; set; }
    public string CurrentCulture { get; set; } = "en";
    public Guid DraftId { get; set; }

    public class InputModel
    {
        [Required]
        public string RecordId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string FrameworkVersion { get; set; } = "1.0";

        [Required]
        public string OrganizationName { get; set; } = string.Empty;

        public string AssessorName { get; set; } = string.Empty;

        [Required]
        public string AssessorRole { get; set; } = string.Empty;

        [Required]
        public string AssessorOrganization { get; set; } = string.Empty;

        [Required]
        public string AssessmentType { get; set; } = string.Empty;

        public List<LayerModel> Layers { get; set; } = new();

        [Required]
        public string Layer1GapNarrative { get; set; } = string.Empty;

        [Required]
        public string Layer2GapNarrative { get; set; } = string.Empty;

        [Required]
        public string Layer3GapNarrative { get; set; } = string.Empty;

        public string? SignatureData { get; set; }

        public void Initialize()
        {
            if (Layers.Count == 0)
            {
                // Layer 1
                Layers.Add(
                    new LayerModel
                    {
                        Name = "Layer 1: Training",
                        Domains = new List<DomainModel>
                        {
                            new() { Id = "1.1", Name = "Dataset Curation" },
                            new() { Id = "1.2", Name = "Category Design" },
                            new() { Id = "1.3", Name = "Training Architecture" },
                            new() { Id = "1.4", Name = "Representational Review" },
                        },
                    }
                );

                // Layer 2
                Layers.Add(
                    new LayerModel
                    {
                        Name = "Layer 2: Fine-Tuning",
                        Domains = new List<DomainModel>
                        {
                            new()
                            {
                                Id = "2.1",
                                Name = "Proprietary Data Selection",
                                CanBeNA = true,
                            },
                            new()
                            {
                                Id = "2.2",
                                Name = "Fine-Tuning Objectives",
                                CanBeNA = true,
                            },
                            new()
                            {
                                Id = "2.3",
                                Name = "Customization Scope",
                                CanBeNA = true,
                            },
                            new()
                            {
                                Id = "2.4",
                                Name = "RAG Configuration",
                                CanBeNA = true,
                            },
                        },
                    }
                );

                // Layer 3
                Layers.Add(
                    new LayerModel
                    {
                        Name = "Layer 3: Deployment",
                        Domains = new List<DomainModel>
                        {
                            new() { Id = "3.1", Name = "Vendor Selection" },
                            new() { Id = "3.2", Name = "Use Case Authorization" },
                            new() { Id = "3.3", Name = "Deployment Scoping" },
                            new() { Id = "3.4", Name = "Accountability Boundaries" },
                        },
                    }
                );
            }
        }
    }

    public class LayerModel
    {
        public string Name { get; set; } = string.Empty;
        public List<DomainModel> Domains { get; set; } = new();
        public string? LayerScore { get; set; }
    }

    public class DomainModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsNotApplicable { get; set; }
        public bool CanBeNA { get; set; }
        public string? NaDocumentation { get; set; }
        public List<LevelCheck> Levels { get; set; } =
            new()
            {
                new() { Level = 0 },
                new() { Level = 1 },
                new() { Level = 2 },
                new() { Level = 3 },
            };
        public string? DomainScore { get; set; }
    }

    public class LevelCheck
    {
        public int Level { get; set; }
        public bool? Answer { get; set; } // null = unanswered, true = Yes, false = No
    }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentCulture = HttpContext.Items["culture"]?.ToString() ?? "en";
        var user = await _userManager.GetUserAsync(User);
        if (user == null || !user.EmailConfirmed)
        {
            return RedirectToPage("/Account/Login", new { culture = CurrentCulture });
        }

        Input.AssessorName = user.Email ?? string.Empty;
        Input.FrameworkVersion = _configuration["Framework:Version"] ?? "1.0";
        Input.RecordId = $"{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}";
        Input.Initialize();

        var draft = await _draftService.GetDraftAsync(user.Id, FormType.MaturityAssessment);
        if (draft != null)
        {
            DraftId = draft.Id;
            var json = _encryptionService.Decrypt(draft.FormData);
            var savedInput = JsonSerializer.Deserialize<InputModel>(json);
            if (savedInput != null)
            {
                Input = savedInput;
                LastSaved = draft.LastSavedAt.ToString("g");
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveDraftAsync([FromBody] InputModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var json = JsonSerializer.Serialize(model);
        var encryptedJson = _encryptionService.Encrypt(json);

        await _draftService.SaveDraftAsync(user.Id, FormType.MaturityAssessment, encryptedJson);
        return new JsonResult(new { success = true, lastSaved = DateTime.UtcNow.ToString("g") });
    }

    public async Task<IActionResult> OnPostAsync(string action)
    {
        CurrentCulture = HttpContext.Items["culture"]?.ToString() ?? "en";
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        Input.Initialize();

        var draft = await _draftService.GetDraftAsync(user.Id, FormType.MaturityAssessment);
        if (draft != null)
        {
            DraftId = draft.Id;
        }

        if (action == "preview")
        {
            IsPreview = true;
            return Page();
        }

        if (action == "edit")
        {
            IsPreview = false;
            return Page();
        }

        if (action == "submit")
        {
            if (!ModelState.IsValid)
            {
                IsPreview = true;
                return Page();
            }

            if (string.IsNullOrEmpty(Input.SignatureData))
            {
                ModelState.AddModelError("Input.SignatureData", _localizer["SignatureRequired"]);
                IsPreview = true;
                return Page();
            }

            var formDataJson = JsonSerializer.Serialize(Input);
            var log = await _submissionService.LogSubmissionAsync(
                FormType.MaturityAssessment,
                formDataJson
            );
            var submissionId = log.Id;
            var contentHash = log.ContentHash;

            var pdfBytes = await _pdfService.GenerateSubmissionPdfAsync(
                FormType.MaturityAssessment,
                formDataJson,
                submissionId,
                contentHash,
                user.Email!,
                DateTime.UtcNow,
                Input.SignatureData
            );

            await _emailService.SendFormSubmissionAsync(
                user.Email!,
                submissionId,
                FormType.MaturityAssessment,
                pdfBytes
            );

            await _draftService.DeleteDraftAsync(user.Id, FormType.MaturityAssessment);

            return File(pdfBytes, "application/pdf", $"MaturityAssessment_{submissionId}.pdf");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid draftId)
    {
        CurrentCulture = HttpContext.Items["culture"]?.ToString() ?? "en";
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        await _draftService.DeleteDraftAsync(user.Id, FormType.MaturityAssessment);

        return RedirectToPage("/Framework/Index", new { culture = CurrentCulture });
    }
}
