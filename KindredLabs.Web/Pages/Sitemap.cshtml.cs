using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KindredLabs.Web.Pages;

/// <summary>
/// Page model for generating the XML sitemap.
/// </summary>
public class SitemapModel : PageModel
{
    private const string BaseUrl = "https://www.kindredlabsfoundation.org";
    private readonly string[] _cultures = { "en", "es" };
    private readonly string[] _pages =
    {
        "",
        "Codex",
        "Framework",
        "CDRP",
        "About",
        "Contact",
        "Privacy",
        "Terms"
    };

    /// <summary>
    /// Gets the list of URLs to include in the sitemap.
    /// </summary>
    public List<string> SitemapUrls { get; private set; } = new();

    /// <summary>
    /// Handles GET requests to populate the sitemap URLs.
    /// </summary>
    public void OnGet()
    {
        foreach (var culture in _cultures)
        {
            foreach (var page in _pages)
            {
                var path = string.IsNullOrEmpty(page) ? $"/{culture}" : $"/{culture}/{page}";
                SitemapUrls.Add($"{BaseUrl}{path}");
            }
        }
    }
}
