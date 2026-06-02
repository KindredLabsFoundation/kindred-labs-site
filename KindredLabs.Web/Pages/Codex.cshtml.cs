using System.Globalization;
using Markdig;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages;

/// <summary>
/// Page model for the Codex page, responsible for rendering markdown content and providing SEO metadata.
/// </summary>
public class CodexModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Gets the localizer for the Codex page.
    /// </summary>
    public IStringLocalizer<Resources.Pages.Codex> Localizer { get; }

    /// <summary>
    /// Gets the rendered HTML content of the Codex.
    /// </summary>
    public string CodexHtml { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodexModel"/> class.
    /// </summary>
    /// <param name="localizer">The string localizer.</param>
    /// <param name="environment">The web host environment.</param>
    public CodexModel(
        IStringLocalizer<Resources.Pages.Codex> localizer,
        IWebHostEnvironment environment
    )
    {
        Localizer = localizer;
        _environment = environment;
    }

    /// <summary>
    /// Handles the GET request, reads the codex markdown file, and converts it to HTML.
    /// </summary>
    public void OnGet()
    {
        ViewData["Title"] = Localizer["PageTitle"];
        ViewData["Description"] = Localizer["PageDescription"];
        ViewData["CanonicalUrl"] = Localizer["CanonicalUrl"];

        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var culturePath = Path.Combine(_environment.WebRootPath, "content", $"codex.{culture}.md");
        var defaultPath = Path.Combine(_environment.WebRootPath, "content", "codex.md");
        var filePath = System.IO.File.Exists(culturePath) ? culturePath : defaultPath;

        if (System.IO.File.Exists(filePath))
        {
            var markdown = System.IO.File.ReadAllText(filePath);
            var pipeline = new MarkdownPipelineBuilder()
                .UseGenericAttributes()
                .UseAdvancedExtensions()
                .Build();
            CodexHtml = Markdown.ToHtml(markdown, pipeline);
        }
    }
}
