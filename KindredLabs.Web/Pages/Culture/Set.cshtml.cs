using KindredLabs.Web.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KindredLabs.Web.Pages.Culture;

public class SetModel : PageModel
{
    public IActionResult OnPost(string culture, string returnUrl)
    {
        if (!SupportedCultures.All.Contains(culture))
        {
            culture = SupportedCultures.Default;
        }

        Response.Cookies.Append(
            "CulturePreference",
            culture,
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" }
        );

        // Swap culture segment in returnUrl
        if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
        {
            var segments = returnUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0 && SupportedCultures.All.Contains(segments[0].ToLower()))
            {
                segments[0] = culture;
                returnUrl = "/" + string.Join("/", segments);
            }
            else
            {
                returnUrl = $"/{culture}{returnUrl}";
            }
            return LocalRedirect(returnUrl);
        }

        return LocalRedirect($"/{culture}/");
    }
}
