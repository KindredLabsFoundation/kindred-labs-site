using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Implementations;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Localization;
using KindredLabs.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
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
        options.SignIn.RequireConfirmedEmail = false;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/en/Account/Login";
    options.Events.OnRedirectToLogin = context =>
    {
        var segments = context.Request.Path.Value?.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries
        );
        var culture =
            segments?.Length > 0
                ? SupportedCultures.Normalize(segments[0])
                : SupportedCultures.Default;
        var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
        context.Response.Redirect($"/{culture}/Account/Login?ReturnUrl={returnUrl}");
        return Task.CompletedTask;
    };
});

// Localization
builder.Services.AddLocalization( /*options => options.ResourcesPath = "Resources"*/
);

builder
    .Services.AddRazorPages(options =>
    {
        options.Conventions.AddFolderRouteModelConvention(
            "/",
            model =>
            {
                foreach (var selector in model.Selectors)
                {
                    selector.AttributeRouteModel!.Template =
                        Microsoft.AspNetCore.Mvc.ApplicationModels.AttributeRouteModel.CombineTemplates(
                            "{culture}",
                            selector.AttributeRouteModel.Template
                        );
                }
            }
        );
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Services
builder.Services.AddTransient<
    Microsoft.AspNetCore.Identity.UI.Services.IEmailSender,
    EmailSenderAdapter
>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IDraftService, DraftService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ICdrpCandidateService, CdrpCandidateService>();
builder.Services.AddScoped<ICommentPeriodService, CommentPeriodService>();
builder.Services.AddScoped<IAdminRoleService, AdminRoleService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
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
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(SupportedCultures.Default)
    .AddSupportedCultures(SupportedCultures.All)
    .AddSupportedUICultures(SupportedCultures.All);

localizationOptions.RequestCultureProviders.Clear();
localizationOptions.RequestCultureProviders.Add(
    new CustomRequestCultureProvider(context =>
    {
        var segments = context.Request.Path.Value?.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries
        );
        var culture =
            segments?.Length > 0
                ? SupportedCultures.Normalize(segments[0])
                : SupportedCultures.Default;
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(culture));
    })
);
localizationOptions.RequestCultureProviders.Add(
    new CookieRequestCultureProvider { CookieName = "CulturePreference" }
);
localizationOptions.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());

app.UseStaticFiles();
app.UseRequestLocalization(localizationOptions);

// Middleware to sync PreferredLocale with current culture
app.Use(
    async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var segments = context.Request.Path.Value?.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries
            );
            if (segments?.Length > 0)
            {
                var currentCulture = SupportedCultures.Normalize(segments[0]);
                var userManager = context.RequestServices.GetRequiredService<
                    UserManager<ApplicationUser>
                >();
                var user = await userManager.GetUserAsync(context.User);

                if (user != null && user.PreferredLocale != currentCulture)
                {
                    user.PreferredLocale = currentCulture;
                    await userManager.UpdateAsync(user);
                }
            }
        }
        await next();
    }
);

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

//app.MapStaticAssets();
app.MapGet(
    "/",
    (HttpRequest request) =>
    {
        var culture = SupportedCultures.Default;

        if (request.Cookies.TryGetValue("CulturePreference", out var cookieCulture))
            culture = SupportedCultures.Normalize(cookieCulture);
        else
        {
            var acceptLanguage = request
                .Headers["Accept-Language"]
                .ToString()
                .Split(',')
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(acceptLanguage))
                culture = SupportedCultures.Normalize(acceptLanguage);
        }

        return Results.Redirect($"/{culture}");
    }
);
app.MapRazorPages() /*.WithStaticAssets()*/
;

app.Run();
