using System.ComponentModel;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace KindredLabs.Tests.E2E.Infrastructure;

[NUnit.Framework.Category("E2E")]
public abstract class PlaywrightTestBase : PageTest
{
    protected string BaseUrl { get; set; } = "http://localhost:5202";

    [SetUp]
    public async Task SetupPlaywrightAsync()
    {
        // PageTest handles browser and context initialization
    }

    [TearDown]
    public async Task TeardownPlaywrightAsync()
    {
        if (Context != null)
        {
            await Context.CloseAsync();
        }
    }

    protected async Task LoginAsync(string email, string password)
    {
        await Page.GotoAsync($"{BaseUrl}/en/Account/Login");
        await Page.FillAsync("input[name='Input.Email']", email);
        await Page.FillAsync("input[name='Input.Password']", password);
        await Page.ClickAsync("button[type='submit']");
    }
}
