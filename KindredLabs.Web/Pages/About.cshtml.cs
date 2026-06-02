using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages;

/// <summary>
/// Model for the About page, providing localization and SEO metadata.
/// </summary>
public class AboutModel : PageModel
{
    /// <summary>
    /// Gets the localizer for the About page.
    /// </summary>
    public IStringLocalizer<Resources.Pages.About> Localizer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutModel"/> class.
    /// </summary>
    /// <param name="localizer">The string localizer.</param>
    public AboutModel(IStringLocalizer<Resources.Pages.About> localizer)
    {
        Localizer = localizer;
    }

    /// <summary>
    /// Handles the GET request and sets SEO metadata in ViewData.
    /// </summary>
    public void OnGet()
    {
        ViewData["Title"] = Localizer["PageTitle"];
        ViewData["Description"] = Localizer["PageDescription"];
        ViewData["CanonicalUrl"] = Localizer["CanonicalUrl"];
    }
}
