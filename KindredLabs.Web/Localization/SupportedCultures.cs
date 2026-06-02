namespace KindredLabs.Web.Localization;

public static class SupportedCultures
{
    public static readonly string[] All = ["en", "es"];
    public static readonly string Default = "en";

    public static string Normalize(string? requested)
    {
        if (string.IsNullOrEmpty(requested))
            return Default;
        var match = All.FirstOrDefault(c =>
            requested.StartsWith(c, StringComparison.OrdinalIgnoreCase)
        );
        return match ?? Default;
    }
}
