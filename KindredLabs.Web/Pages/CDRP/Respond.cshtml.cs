using System.Text.Json;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.CDRP;

/// <summary>
/// Page model for the CDRP supplementary response page.
/// </summary>
public class RespondModel : PageModel
{
    private readonly ICdrpCandidateService _candidateService;
    private readonly IEncryptionService _encryptionService;
    private readonly IStringLocalizer<Resources.Pages.CDRP.Respond> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RespondModel"/> class.
    /// </summary>
    public RespondModel(
        ICdrpCandidateService candidateService,
        IEncryptionService encryptionService,
        IStringLocalizer<Resources.Pages.CDRP.Respond> localizer
    )
    {
        _candidateService = candidateService;
        _encryptionService = encryptionService;
        _localizer = localizer;
    }

    public IStringLocalizer<Resources.Pages.CDRP.Respond> Localizer => _localizer;

    public CdrpCandidate? Candidate { get; set; }
    public Dictionary<string, string> CandidateFields { get; set; } = [];
    public Dictionary<string, SupplementaryEntry> SupplementaryData { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    public bool IsSuccess { get; set; }

    public class SupplementaryEntry
    {
        public string Question { get; set; } = null!;
        public string? Response { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            return Page();
        }

        Candidate = await _candidateService.GetCandidateByTokenAsync(Token);
        if (Candidate != null)
        {
            // Candidate.FormData is already decrypted by GetCandidateByTokenAsync
            var rawFields = JsonSerializer.Deserialize<Dictionary<string, object>>(
                Candidate.FormData
            );
            CandidateFields =
                rawFields
                    ?.Where(kvp => kvp.Key != "Acknowledgment")
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                ?? [];
            if (!string.IsNullOrEmpty(Candidate.SupplementaryData))
            {
                var allSupplementary =
                    JsonSerializer.Deserialize<Dictionary<string, SupplementaryEntry>>(
                        Candidate.SupplementaryData
                    ) ?? [];

                // Only include entries that actually have a question
                SupplementaryData = allSupplementary
                    .Where(kvp => !string.IsNullOrEmpty(kvp.Value.Question))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            // Filter CandidateFields to only include fields that have a non-empty question
            CandidateFields = CandidateFields
                .Where(kvp => SupplementaryData.ContainsKey(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string token, Dictionary<string, string> responses)
    {
        if (string.IsNullOrEmpty(token))
        {
            return Page();
        }

        // We fetch the candidate BEFORE saving responses because SaveSupplementaryResponsesAsync clears the token
        Candidate = await _candidateService.GetCandidateByTokenAsync(token);

        if (Candidate == null)
        {
            return Page();
        }

        try
        {
            await _candidateService.SaveSupplementaryResponsesAsync(token, responses);
            IsSuccess = true;
        }
        catch (KeyNotFoundException)
        {
            Candidate = null;
        }

        return Page();
    }
}
