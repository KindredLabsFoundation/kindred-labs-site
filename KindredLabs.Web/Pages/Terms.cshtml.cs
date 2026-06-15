using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages;

/// <summary>
/// Model for the Terms of Service page.
/// </summary>
public class TermsModel : PageModel
{
    /// <summary>
    /// Gets the localizer for the Terms page.
    /// </summary>
    public IStringLocalizer<Resources.Pages.Terms> Localizer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TermsModel"/> class.
    /// </summary>
    /// <param name="localizer">The localizer.</param>
    public TermsModel(IStringLocalizer<Resources.Pages.Terms> localizer)
    {
        Localizer = localizer;
    }

    /// <summary>
    /// Handles GET requests for the Terms of Service page.
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
