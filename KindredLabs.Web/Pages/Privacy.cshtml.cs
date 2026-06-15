using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages;

/// <summary>
/// Model for the Privacy Policy page.
/// </summary>
public class PrivacyModel : PageModel
{
    /// <summary>
    /// Gets the localizer for the Privacy page.
    /// </summary>
    public IStringLocalizer<Resources.Pages.Privacy> Localizer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrivacyModel"/> class.
    /// </summary>
    /// <param name="localizer">The localizer.</param>
    public PrivacyModel(IStringLocalizer<Resources.Pages.Privacy> localizer)
    {
        Localizer = localizer;
    }

    /// <summary>
    /// Handles GET requests for the Privacy Policy page.
    /// </summary>
    /// <param name="embed">Whether the page is embedded in an iframe.</param>
    public void OnGet(bool embed = false)
    {
        if (embed)
        {
            ViewData["Layout"] = null;
        }
    }
}
