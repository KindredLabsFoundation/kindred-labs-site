using System.Net;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Tests.E2E.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace KindredLabs.Tests.E2E.Integration;

[TestFixture]
[Category("Integration")]
public class AuthenticationTests : IntegrationTestBase
{
    [Test]
    public async Task CultureRouting_ReturnsCorrectStatusAndRedirects()
    {
        // GET /en/Account/Login returns 200
        var enResponse = await Client.GetAsync("/en/Account/Login");
        Assert.That(enResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // GET /es/Account/Login returns 200
        var esResponse = await Client.GetAsync("/es/Account/Login");
        Assert.That(esResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // GET /Account/Login (no culture) redirects to /en/Account/Login
        var clientNoAutoRedirect = Factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            }
        );
        var noCultureResponse = await clientNoAutoRedirect.GetAsync("/");
        // It should be 301 or 302, Redirect is 302
        Assert.That(noCultureResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(noCultureResponse.Headers.Location?.OriginalString.TrimEnd('/'), Does.EndWith("/en"));
    }

    [Test]
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
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)); // It might return OK if it shows "Confirmation sent" page

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);

        Assert.That(user, Is.Not.Null);
        Assert.That(user.FirstName, Is.EqualTo("John"));
        Assert.That(user.LastName, Is.EqualTo("Doe"));
        Assert.That(user.Organization, Is.EqualTo("Kindred Labs"));
        Assert.That(user.JobTitle, Is.EqualTo("Developer"));
    }

    [Test]
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
        Assert.That(location, Does.Contain("/en/Account/LoginWith2fa"));
    }

    [Test]
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

        Assert.That(async () => await context.SaveChangesAsync(), Throws.InstanceOf<DbUpdateException>());
    }

    [Test]
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
            Assert.That(userFound, Is.False);

            var draftsCount = await db.Drafts.CountAsync(d => d.UserId == userId);
            Assert.That(draftsCount, Is.EqualTo(0));

            var submissionLog = await db
                .SubmissionLogs.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ContentHash == "hash-to-delete");
            Assert.That(submissionLog, Is.Not.Null);
            Assert.That(submissionLog.UserId, Is.Null);
        }
    }

    [Test]
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

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        var location = response.Headers.Location?.OriginalString;
        Assert.That(location, Does.Contain("/en/Account/Login"));

        // GET /es/Account/Index while unauthenticated
        var esResponse = await clientNoAutoRedirect.GetAsync("/es/Account/Index");
        Assert.That(esResponse.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(esResponse.Headers.Location?.OriginalString, Does.Contain("/es/Account/Login"));
    }
}
