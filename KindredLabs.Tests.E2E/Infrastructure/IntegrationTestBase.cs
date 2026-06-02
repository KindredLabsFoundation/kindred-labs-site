using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;

namespace KindredLabs.Tests.E2E.Infrastructure;

[IntegrationTest]
public abstract class IntegrationTestBase
    : IClassFixture<KindredLabsWebApplicationFactory>,
        IAsyncLifetime
{
    protected readonly KindredLabsWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(KindredLabsWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
            }
        );
    }

    public virtual async Task InitializeAsync()
    {
        // Initialization if needed
    }

    public virtual async Task DisposeAsync()
    {
        await Factory.CleanDatabaseAsync();
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
