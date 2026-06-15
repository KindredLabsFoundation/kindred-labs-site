using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KindredLabs.Core.Services.Interfaces;

namespace KindredLabs.Tests.E2E.Infrastructure;

public class KindredLabsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                var testSettingsPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "appsettings.test.json"
                );
                config.AddJsonFile(testSettingsPath, optional: false);
            }
        );

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
            );

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext using TestConnection
            services.AddDbContext<ApplicationDbContext>(
                (sp, options) =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    options.UseNpgsql(config.GetConnectionString("TestConnection"));
                }
            );

            // Override IEmailService with NoOpEmailService
            services.AddScoped<IEmailService, NoOpEmailService>();

            // Ensure database is migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }

    public async Task CleanDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Delete all data from tables
        // Order matters for FK constraints
        db.AdminAuditLogs.RemoveRange(db.AdminAuditLogs);
        db.CdrpCandidates.RemoveRange(db.CdrpCandidates);
        db.Drafts.RemoveRange(db.Drafts);
        db.SubmissionLogs.RemoveRange(db.SubmissionLogs);
        db.CommentPeriods.RemoveRange(db.CommentPeriods);
        db.LoginHistories.RemoveRange(db.LoginHistories);
        db.SecurityAuditLogs.RemoveRange(db.SecurityAuditLogs);
        db.UserEmails.RemoveRange(db.UserEmails);
        await db.SaveChangesAsync();

        // Remove users directly to bypass UserManager restrictions in tests
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }

    public async Task SeedTestUserAsync(string email, string password)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
            };
            await userManager.CreateAsync(user, password);
        }
    }
}
