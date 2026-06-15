using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Framework;

/// <summary>
///  Page model for the Upstream Governance Framework
/// </summary>
public class IndexModel : PageModel
{
    /// <summary>
    /// Gets the localizer for the Framework page
    /// </summary>
    public IStringLocalizer<Resources.Pages.Framework.Index> Localizer { get; }

    public Dictionary<string, string> DiscussionUrls { get; private set; } = new();
    private readonly IMemoryCache _cache;
    private readonly ICommentPeriodService _commentPeriodService;

    /// <summary>
    /// Gets the active comment period, if any.
    /// </summary>
    public CommentPeriod? ActiveCommentPeriod { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="localizer"></param>
    /// <param name="commentPeriodService"></param>
    /// <param name="cache"></param>
    public IndexModel(
        IStringLocalizer<Resources.Pages.Framework.Index> localizer,
        ICommentPeriodService commentPeriodService,
        IMemoryCache cache
        )
    {
        Localizer = localizer;
        _commentPeriodService = commentPeriodService;
        _cache = cache;
    }

    /// <summary>
    /// Handles the HTTP GET request and sets SEO metadata in ViewData
    /// </summary>
    public async Task OnGetAsync()
    {
        ViewData["Title"] = Localizer["PageTitle"];
        ViewData["Description"] = Localizer["PageDescription"];
        ViewData["CanonicalUrl"] = Localizer["CanonicalUrl"];
        ViewData["JsonLd"] = $@"{{
  ""@context"": ""https://schema.org"",
  ""@type"": ""WebPage"",
  ""name"": ""{ViewData["Title"]}"",
  ""url"": ""{ViewData["CanonicalUrl"]}""
}}";

        ActiveCommentPeriod = await _commentPeriodService.GetActiveCommentPeriodAsync();
    }

    /// <summary>
    /// Gets the discussion URLs for the framework sections.
    /// </summary>
    /// <returns>A JSON result containing a dictionary of section slugs and their corresponding GitHub discussion URLs.</returns>
    public async Task<JsonResult> OnGetDiscussionUrlsAsync()
    {
        if (!_cache.TryGetValue("FrameworkDiscussionUrls", out Dictionary<string, string>? cached) || cached == null)
        {
            var isOpen = await _commentPeriodService.IsCommentPeriodOpenAsync();
            var slugs = new[]
            {
                "executive-summary", "framework-introduction", "training", "fine-tuning",
                "deployment", "layer-integration", "disclosure-requirements-at-selection",
                "ongoing-transparency-obligations", "contractual-accountability-structures",
                "vendor-evaluation-criteria", "data-provenance-requirements",
                "consent-documentation-standards", "category-review-process",
                "ongoing-audit-requirements", "maturity-levels", "scoring-architecture",
                "assessment-instrument", "scoring-and-output-format", "rubric-revision-protocol",
                "standards-mapping", "appendix-a", "appendix-b", "appendix-c"
            };
            const string baseUrl = "https://github.com/KindredLabsFoundation/upstream-governance-framework/discussions/categories/";
            cached = slugs.ToDictionary(
                s => s,
                s => isOpen ? baseUrl + s : "#"
            );
            _cache.Set("FrameworkDiscussionUrls", cached, TimeSpan.FromHours(1));
        }
        return new JsonResult(cached);
    }
}