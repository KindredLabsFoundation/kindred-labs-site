using System.Net;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace KindredLabs.Tests.E2E.Infrastructure;

[TestFixture]
public abstract class IntegrationTestBase
{
    protected KindredLabsWebApplicationFactory Factory = null!;
    protected HttpClient Client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Factory = new KindredLabsWebApplicationFactory();
        Client = Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
            }
        );
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        await Factory.CleanDatabaseAsync();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Client.Dispose();
        Factory.Dispose();
    }

    protected async Task LoginAsTestUserAsync(
        string email = "test@kindredlabsfoundation.org",
        string password = "Password123!"
    )
    {
        await Factory.SeedTestUserAsync(email, password);

        var antiForgeryToken = await GetAntiForgeryTokenAsync("/en/Account/Login");
        var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "Input.Email", email },
                { "Input.Password", password },
                { "Input.RememberMe", "false" },
                { "__RequestVerificationToken", antiForgeryToken },
            }
        );

        await Client.PostAsync("/en/Account/Login", content);
    }

    protected async Task<string> GetAntiForgeryTokenAsync(string url)
    {
        var response = await Client.GetAsync(url);
        var responseContent = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(
            responseContent,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />"
        );
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Try standard ASP.NET Core hidden field pattern
        match = Regex.Match(
            responseContent,
            @"input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"""
        );
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        throw new InvalidOperationException(
            $"Anti-forgery token not found in response from {url}. Body: {responseContent}"
        );
    }
}
