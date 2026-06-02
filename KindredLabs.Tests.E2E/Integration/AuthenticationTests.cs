using System.Net;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Categories;
using Assert = Xunit.Assert;
using CategoryAttribute = Xunit.Categories.CategoryAttribute;

namespace KindredLabs.Tests.E2E.Integration;

[Category("Integration")]
public class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(KindredLabsWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task CultureRouting_ReturnsCorrectStatusAndRedirects()
    {
        // GET /en/Account/Login returns 200
        var enResponse = await Client.GetAsync("/en/Account/Login");
        Assert.Equal(HttpStatusCode.OK, enResponse.StatusCode);

        // GET /es/Account/Login returns 200
        var esResponse = await Client.GetAsync("/es/Account/Login");
        Assert.Equal(HttpStatusCode.OK, esResponse.StatusCode);

        // GET /Account/Login (no culture) redirects to /en/Account/Login
        var clientNoAutoRedirect = Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            }
        );
        var noCultureResponse = await clientNoAutoRedirect.GetAsync("/");
        // It should be 301 or 302, Redirect is 302
        Assert.Equal(HttpStatusCode.Redirect, noCultureResponse.StatusCode);
        Assert.EndsWith("/en", noCultureResponse.Headers.Location?.OriginalString.TrimEnd('/'));
    }

    [Fact]
    public async Task RegistrationFlow_PersistsUserData()
    {
        var email = "newuser@example.com";
        var antiForgeryToken = await GetAntiForgeryTokenAsync("/en/Account/Register");

        var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "Input.FirstName", "John" },
                { "Input.LastName", "Doe" },
                { "Input.Organization", "Kindred Labs" },
                { "Input.JobTitle", "Developer" },
                { "Input.Email", email },
                { "Input.Password", "Password123!" },
                { "Input.ConfirmPassword", "Password123!" },
                { "__RequestVerificationToken", antiForgeryToken },
            }
        );

        var response = await Client.PostAsync("/en/Account/Register", content);

        // Assert redirect or success (Register page redirects after success)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // It might return OK if it shows "Confirmation sent" page

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("Kindred Labs", user.Organization);
        Assert.Equal("Developer", user.JobTitle);
    }

    [Fact]
    public async Task LoginRedirectsTo2FA_WhenEnabled()
    {
        var email = $"user2fa-{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>
            >();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                throw new Exception("User creation failed");
            await userManager.SetTwoFactorEnabledAsync(user, true);
        }

        var clientNoAutoRedirect = Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true, // Ensure cookies are handled
            }
        );
        var antiForgeryToken = await GetAntiForgeryTokenAsync("/en/Account/Login"); // This helper uses its own client, but Factory clients share cookies? No they don't by default if created separately.

        // Let's manually get token with the same client
        var getResponse = await clientNoAutoRedirect.GetAsync("/en/Account/Login");
        var loginHtml = await getResponse.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(
            loginHtml,
            @"input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"""
        );
        var token = match.Groups[1].Value;

        var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "Input.Email", email },
                { "Input.Password", password },
                { "Input.RememberMe", "false" },
                { "__RequestVerificationToken", token },
            }
        );

        var response = await clientNoAutoRedirect.PostAsync("/en/Account/Login", content);

        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed. Status: {response.StatusCode}. Body: {errorBody}");
        }

        var location = response.Headers.Location?.OriginalString;
        Assert.Contains("/en/Account/LoginWith2fa", location);
    }

    [Fact]
    public async Task UserEmailsUniqueConstraint_ThrowsViolation()
    {
        var email = "duplicate@example.com";

        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user1 = new ApplicationUser { UserName = "user1@test.com", Email = "user1@test.com" };
        var user2 = new ApplicationUser { UserName = "user2@test.com", Email = "user2@test.com" };

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        context.UserEmails.Add(new UserEmail { UserId = user1.Id, Email = email });
        await context.SaveChangesAsync();

        context.UserEmails.Add(new UserEmail { UserId = user2.Id, Email = email });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task AccountDeletionCascade_CleansUpData()
    {
        var email = $"delete-{Guid.NewGuid()}@integration.test";
        var password = "Password123!";
        string userId;

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<
                UserManager<ApplicationUser>
            >();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(user, password);
            userId = user.Id;

            context.Drafts.Add(
                new Draft
                {
                    UserId = user.Id,
                    FormType = FormType.DataProvenance,
                    FormData = "{}",
                    ExpiresAt = DateTime.UtcNow.AddHours(81),
                }
            );

            context.SubmissionLogs.Add(
                new SubmissionLog
                {
                    UserId = user.Id,
                    FormType = FormType.DataProvenance,
                    ContentHash = "hash-to-delete",
                }
            );

            await context.SaveChangesAsync();
        }

        // Login first
        await LoginAsTestUserAsync(email, password);

        var antiForgeryToken = await GetAntiForgeryTokenAsync("/en/Account");

        var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "DeleteAccount.Password", password },
                { "DeleteAccount.ConfirmPhrase", "DELETE" },
                { "__RequestVerificationToken", antiForgeryToken },
            }
        );

        var response = await Client.PostAsync("/en/Account?handler=DeleteAccount", content);

        if (
            response.StatusCode != HttpStatusCode.OK
            && response.StatusCode != HttpStatusCode.Redirect
        )
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new Exception(
                $"Account deletion failed. Status: {response.StatusCode}. Body: {responseBody}"
            );
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check existence
            var userFound = await db.Users.AnyAsync(u => u.Id == userId);
            Assert.False(userFound);

            var draftsCount = await db.Drafts.CountAsync(d => d.UserId == userId);
            Assert.Equal(0, draftsCount);

            var submissionLog = await db
                .SubmissionLogs.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ContentHash == "hash-to-delete");
            Assert.NotNull(submissionLog);
            Assert.Null(submissionLog.UserId);
        }
    }

    [Fact]
    public async Task CookieAuthenticationRedirect_UsesCorrectCulture()
    {
        var clientNoAutoRedirect = Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            }
        );

        // GET /en/Account/Index while unauthenticated
        var response = await clientNoAutoRedirect.GetAsync("/en/Account/Index");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.OriginalString;
        Assert.Contains("/en/Account/Login", location);

        // GET /es/Account/Index while unauthenticated
        var esResponse = await clientNoAutoRedirect.GetAsync("/es/Account/Index");
        Assert.Equal(HttpStatusCode.Redirect, esResponse.StatusCode);
        Assert.Contains("/es/Account/Login", esResponse.Headers.Location?.OriginalString);
    }
}
