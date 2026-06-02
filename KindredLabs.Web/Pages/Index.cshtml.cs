using KindredLabs.Web.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages;

public class IndexModel : PageModel
{
    public IStringLocalizer<Resources.Pages.Index> Localizer { get; }

    public IndexModel(IStringLocalizer<Resources.Pages.Index> localizer)
    {
        Localizer = localizer;
    }

    public IActionResult OnGet()
    {
        // If no culture in route, detect and redirect
        if (!RouteData.Values.ContainsKey("culture"))
        {
            var culture = SupportedCultures.Default;

            // Check cookie
            if (Request.Cookies.TryGetValue("CulturePreference", out var cookieCulture))
                culture = SupportedCultures.Normalize(cookieCulture);
            else
            {
                // Check Accept-Language header
                var acceptLanguage = Request
                    .Headers["Accept-Language"]
                    .ToString()
                    .Split(',')
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(acceptLanguage))
                    culture = SupportedCultures.Normalize(acceptLanguage);
            }

            return RedirectToPage("/Index", new { culture });
        }

        ViewData["Title"] = Localizer["PageTitle"];
        ViewData["Description"] = Localizer["PageDescription"];
        ViewData["CanonicalUrl"] = Localizer["CanonicalUrl"];
        return Page();
    }
}
