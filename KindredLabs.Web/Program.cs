using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Implementations;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PostmarkDotNet;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Data Protection -- keys persisted to PostgreSQL
builder
    .Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("KindredLabs");

// Identity
builder
    .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddRazorPages().AddViewLocalization().AddDataAnnotationsLocalization();

// Services
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IDraftService, DraftService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ICdrpCandidateService, CdrpCandidateService>();
builder.Services.AddScoped<ICommentPeriodService, CommentPeriodService>();
builder.Services.AddScoped<IAdminRoleService, AdminRoleService>();
builder.Services.AddHttpClient<IGitHubDiscussionsService, GitHubDiscussionsService>();
builder.Services.AddHostedService<DraftExpiryBackgroundService>();

// Postmark
builder.Services.AddSingleton<PostmarkClient>(_ => new PostmarkClient(
    builder.Configuration["Postmark:ApiKey"]
        ?? throw new InvalidOperationException("Postmark API key not configured.")
));

var app = builder.Build();

// Seed Owner Role
using (var scope = app.Services.CreateScope())
{
    var adminRoleService = scope.ServiceProvider.GetRequiredService<IAdminRoleService>();
    var ownerEmail = app.Configuration["Admin:OwnerEmail"];
    if (!string.IsNullOrEmpty(ownerEmail))
    {
        await adminRoleService.SeedOwnerRoleAsync(ownerEmail);
    }
}

// Localization middleware
var supportedCultures = new[] { "en", "es" };
app.UseRequestLocalization(
    new RequestLocalizationOptions()
        .SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures)
);

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
