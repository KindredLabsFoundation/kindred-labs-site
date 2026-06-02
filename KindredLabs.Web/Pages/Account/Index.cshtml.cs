using System.ComponentModel.DataAnnotations;
using System.Text;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Account;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<Resources.Pages.Account.Index> _localizer;
    private readonly ISecurityService _securityService;
    private readonly ILogger<IndexModel> _logger;

    public IStringLocalizer<Resources.Pages.Account.Index> Localizer => _localizer;

    public string CurrentCulture =>
        HttpContext
            .Features.Get<IRequestCultureFeature>()
            ?.RequestCulture.Culture.TwoLetterISOLanguageName
        ?? "en";

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        IStringLocalizer<Resources.Pages.Account.Index> localizer,
        ISecurityService securityService,
        ILogger<IndexModel> logger
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _localizer = localizer;
        _securityService = securityService;
        _logger = logger;
    }

    [BindProperty]
    public ProfileInput Profile { get; set; } = new();

    public class ProfileInput
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Organization { get; set; }
        public string? JobTitle { get; set; }
    }

    [BindProperty]
    public string PreferredLanguage { get; set; } = "en";

    [BindProperty]
    [EmailAddress]
    public string? NewEmail { get; set; }

    [BindProperty]
    public DeleteAccountInput DeleteAccount { get; set; } = new();

    public class DeleteAccountInput
    {
        public string Password { get; set; } = string.Empty;

        public string ConfirmPhrase { get; set; } = string.Empty;
    }

    public ApplicationUser CurrentUser { get; private set; } = null!;
    public List<UserEmail> AdditionalEmails { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (token != null)
        {
            return await OnGetConfirmAdditionalEmailAsync(token);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );
        }

        await LoadUserDataAsync(user);
        return Page();
    }

    private async Task LoadUserDataAsync(ApplicationUser user)
    {
        CurrentUser = user;
        Profile.FirstName = user.FirstName;
        Profile.LastName = user.LastName;
        Profile.Organization = user.Organization;
        Profile.JobTitle = user.JobTitle;
        PreferredLanguage = user.PreferredLocale ?? "en";

        AdditionalEmails = await _context
            .UserEmails.Where(e => e.UserId == user.Id && !e.IsPrimary)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSaveProfileAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        if (
            string.IsNullOrWhiteSpace(Profile.FirstName)
            && string.IsNullOrWhiteSpace(Profile.LastName)
        )
        {
            ModelState.AddModelError("Profile.FirstName", _localizer["NameRequired"]);
            await LoadUserDataAsync(user);
            return Page();
        }

        user.FirstName = Profile.FirstName;
        user.LastName = Profile.LastName;
        user.Organization = Profile.Organization;
        user.JobTitle = Profile.JobTitle;

        await _userManager.UpdateAsync(user);
        TempData["StatusMessage"] = _localizer["ProfileSavedSuccess"].Value;
        return RedirectToPage(new { culture = CurrentCulture });
    }

    public async Task<IActionResult> OnPostSavePreferencesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        user.PreferredLocale = PreferredLanguage;
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(PreferredLanguage)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                }
            );
            TempData["StatusMessage"] = _localizer["PreferencesUpdated"].Value;
        }
        else
        {
            TempData["ErrorMessage"] = _localizer["PreferencesUpdateFailed"].Value;
        }

        return RedirectToPage(new { culture = PreferredLanguage });
    }

    public async Task<IActionResult> OnPostAddEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        if (string.IsNullOrWhiteSpace(NewEmail))
        {
            ModelState.AddModelError("NewEmail", _localizer["EmailRequired"]);
            await LoadUserDataAsync(user);
            return Page();
        }

        if (!ModelState.IsValid)
        {
            await LoadUserDataAsync(user);
            return Page();
        }

        var token = Guid.NewGuid().ToString();
        var userEmail = new UserEmail
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = NewEmail,
            IsPrimary = false,
            IsVerified = false,
            VerificationToken = token,
            VerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
        };

        _context.UserEmails.Add(userEmail);
        await _context.SaveChangesAsync();

        var confirmationLink = Url?.Page(
            "/Account/Index",
            pageHandler: null,
            values: new { token },
            protocol: Request.Scheme
        );

        try
        {
            if (confirmationLink != null)
            {
                await _emailService.SendAdditionalEmailConfirmationAsync(
                    NewEmail,
                    confirmationLink
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send additional email confirmation to {Email}",
                NewEmail
            );
        }

        TempData["StatusMessage"] = _localizer["EmailAddedSuccess"].Value;
        return RedirectToPage(new { culture = CurrentCulture });
    }

    public async Task<IActionResult> OnGetConfirmAdditionalEmailAsync(string token)
    {
        var userEmail = await _context.UserEmails.FirstOrDefaultAsync(e =>
            e.VerificationToken == token && e.VerificationTokenExpiry > DateTime.UtcNow
        );

        if (userEmail == null)
        {
            TempData["ErrorMessage"] = _localizer["EmailConfirmationFailed"].Value;
            return RedirectToPage(new { culture = CurrentCulture });
        }

        userEmail.IsVerified = true;
        userEmail.VerificationToken = null;
        userEmail.VerificationTokenExpiry = null;

        await _context.SaveChangesAsync();

        TempData["StatusMessage"] = _localizer["EmailConfirmedSuccess"].Value;
        return RedirectToPage(new { culture = CurrentCulture });
    }

    public async Task<IActionResult> OnPostMakePrimaryAsync(Guid emailId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        var newPrimaryEmail = await _context.UserEmails.FirstOrDefaultAsync(e =>
            e.Id == emailId && e.UserId == user.Id && e.IsVerified
        );

        if (newPrimaryEmail == null)
            return RedirectToPage(new { culture = CurrentCulture });

        var oldPrimaryEmail = user.Email!;
        var securityEmail =
            _configuration["Contact:SecurityEmail"] ?? "security@kindredlabsfoundation.org";

        await _emailService.SendPrimaryEmailChangedNotificationAsync(
            oldPrimaryEmail,
            newPrimaryEmail.Email,
            securityEmail
        );

        // Update user
        user.Email = newPrimaryEmail.Email;
        user.UserName = newPrimaryEmail.Email;
        await _userManager.UpdateAsync(user);

        // Update UserEmails
        var oldPrimaryRecord = await _context.UserEmails.FirstOrDefaultAsync(e =>
            e.UserId == user.Id && e.IsPrimary
        );

        if (oldPrimaryRecord != null)
        {
            oldPrimaryRecord.IsPrimary = false;
        }
        else
        {
            // If for some reason it wasn't in UserEmails, add it
            _context.UserEmails.Add(
                new UserEmail
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Email = oldPrimaryEmail,
                    IsPrimary = false,
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }

        newPrimaryEmail.IsPrimary = true;
        await _context.SaveChangesAsync();

        await _userManager.UpdateSecurityStampAsync(user);
        await _signInManager.SignOutAsync();
        TempData["StatusMessage"] = _localizer["PrimaryEmailUpdatedSuccess"].Value;
        return RedirectToPage(
            "/Account/Login",
            new { culture = CurrentCulture, area = "Identity" }
        );
    }

    public async Task<IActionResult> OnPostRemoveEmailAsync(Guid emailId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        var emailToRemove = await _context.UserEmails.FirstOrDefaultAsync(e =>
            e.Id == emailId && e.UserId == user.Id && !e.IsPrimary
        );

        if (emailToRemove != null)
        {
            _context.UserEmails.Remove(emailToRemove);
            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = _localizer["EmailRemovedSuccess"].Value;
        }

        return RedirectToPage(new { culture = CurrentCulture });
    }

    public async Task<IActionResult> OnPostResendVerificationAsync(Guid emailId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        var emailToResend = await _context.UserEmails.FirstOrDefaultAsync(e =>
            e.Id == emailId && e.UserId == user.Id && !e.IsVerified
        );

        if (emailToResend != null)
        {
            emailToResend.VerificationToken = Guid.NewGuid().ToString();
            emailToResend.VerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            var culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            var confirmationLink = Url.Page(
                "/Account/Index",
                pageHandler: "ConfirmAdditionalEmail",
                values: new { culture, token = emailToResend.VerificationToken },
                protocol: Request.Scheme
            );

            try
            {
                await _emailService.SendAdditionalEmailConfirmationAsync(
                    emailToResend.Email,
                    confirmationLink!
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resend additional email confirmation to {Email}",
                    emailToResend.Email
                );
            }

            TempData["StatusMessage"] = _localizer["VerificationResent"].Value;
        }

        return RedirectToPage(new { culture = CurrentCulture });
    }

    public async Task<IActionResult> OnPostRevokeOtherSessionsAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        await _userManager.UpdateSecurityStampAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        await _securityService.RecordAuditEventAsync(
            user.Id,
            SecurityEventTypes.OtherSessionsRevoked,
            "User revoked all other active sessions.",
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        TempData["StatusMessage"] = _localizer["OtherSessionsRevokedSuccess"].Value;
        return RedirectToPage(new { culture = CurrentCulture });
    }

    public async Task<IActionResult> OnGetDownloadDataAsync(string format)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        var isCsv = string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase);
        var sb = new StringBuilder();

        // Profile
        var localeName = user.PreferredLocale == "es" ? "Español" : "English";
        if (isCsv)
        {
            sb.AppendLine("SECTION: PROFILE");
            sb.AppendLine(
                "FirstName,LastName,Email,Organization,JobTitle,PreferredLocale,CreatedAt"
            );
            sb.AppendLine(
                $"{EscapeCsv(user.FirstName)},{EscapeCsv(user.LastName)},{EscapeCsv(user.Email)},{EscapeCsv(user.Organization)},{EscapeCsv(user.JobTitle)},{EscapeCsv(localeName)},{user.CreatedAt:O}"
            );
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("SECTION: PROFILE");
            sb.AppendLine(new string('-', 20));
            sb.AppendLine($"First Name: {user.FirstName}");
            sb.AppendLine($"Last Name: {user.LastName}");
            sb.AppendLine($"Email: {user.Email}");
            sb.AppendLine($"Organization: {user.Organization}");
            sb.AppendLine($"Job Title: {user.JobTitle}");
            sb.AppendLine($"Preferred Locale: {localeName}");
            sb.AppendLine($"Created At: {user.CreatedAt:O}");
            sb.AppendLine();
        }

        // Additional Emails
        var additionalEmails = await _context
            .UserEmails.Where(e => e.UserId == user.Id && !e.IsPrimary)
            .ToListAsync();

        if (isCsv)
        {
            sb.AppendLine("SECTION: ADDITIONAL EMAILS");
            sb.AppendLine("Email,IsVerified,CreatedAt");
            foreach (var email in additionalEmails)
            {
                sb.AppendLine($"{EscapeCsv(email.Email)},{email.IsVerified},{email.CreatedAt:O}");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("SECTION: ADDITIONAL EMAILS");
            sb.AppendLine(new string('-', 20));
            foreach (var email in additionalEmails)
            {
                sb.AppendLine($"Email: {email.Email}");
                sb.AppendLine($"Verified: {email.IsVerified}");
                sb.AppendLine($"Created At: {email.CreatedAt:O}");
                sb.AppendLine(new string('-', 10));
            }
            sb.AppendLine();
        }

        // Login History
        var loginHistory = await _securityService.GetLoginHistoryAsync(user.Id, 1000);
        if (isCsv)
        {
            sb.AppendLine("SECTION: LOGIN HISTORY");
            sb.AppendLine("LoginAt,Success,IpAddress,UserAgent");
            foreach (var item in loginHistory)
            {
                sb.AppendLine(
                    $"{item.LoginAt:O},{item.Success},{EscapeCsv(item.IpAddress)},{EscapeCsv(item.UserAgent)}"
                );
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("SECTION: LOGIN HISTORY");
            sb.AppendLine(new string('-', 20));
            foreach (var item in loginHistory)
            {
                sb.AppendLine($"Time: {item.LoginAt:O}");
                sb.AppendLine($"Success: {item.Success}");
                sb.AppendLine($"IP: {item.IpAddress}");
                sb.AppendLine($"User Agent: {item.UserAgent}");
                sb.AppendLine(new string('-', 10));
            }
            sb.AppendLine();
        }

        // Security Audit Log
        var auditLog = await _securityService.GetAuditLogAsync(user.Id, 1000);
        if (isCsv)
        {
            sb.AppendLine("SECTION: SECURITY AUDIT LOG");
            sb.AppendLine("PerformedAt,EventType,Description,IpAddress");
            foreach (var item in auditLog)
            {
                sb.AppendLine(
                    $"{item.PerformedAt:O},{EscapeCsv(item.EventType)},{EscapeCsv(item.Description)},{EscapeCsv(item.IpAddress)}"
                );
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("SECTION: SECURITY AUDIT LOG");
            sb.AppendLine(new string('-', 20));
            foreach (var item in auditLog)
            {
                sb.AppendLine($"Time: {item.PerformedAt:O}");
                sb.AppendLine($"Event: {item.EventType}");
                sb.AppendLine($"Description: {item.Description}");
                sb.AppendLine($"IP: {item.IpAddress}");
                sb.AppendLine(new string('-', 10));
            }
            sb.AppendLine();
        }

        // Submission Logs
        var submissionLogs = await _context
            .SubmissionLogs.Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        if (isCsv)
        {
            sb.AppendLine("SECTION: SUBMISSION LOGS");
            sb.AppendLine("SubmittedAt,FormType,ContentHash");
            foreach (var item in submissionLogs)
            {
                sb.AppendLine(
                    $"{item.SubmittedAt:O},{item.FormType},{EscapeCsv(item.ContentHash)}"
                );
            }
        }
        else
        {
            sb.AppendLine("SECTION: SUBMISSION LOGS");
            sb.AppendLine(new string('-', 20));
            foreach (var item in submissionLogs)
            {
                sb.AppendLine($"Submitted At: {item.SubmittedAt:O}");
                sb.AppendLine($"Form Type: {item.FormType}");
                sb.AppendLine($"Content Hash: {item.ContentHash}");
                sb.AppendLine(new string('-', 10));
            }
        }

        var content = sb.ToString();
        var bytes = Encoding.UTF8.GetBytes(content);
        var contentType = isCsv ? "text/csv" : "text/plain";
        var extension = isCsv ? "csv" : "txt";
        var fileName = $"kindredlabs_account_data_{DateTime.UtcNow:yyyyMMdd}.{extension}";

        return File(bytes, contentType, (string?)fileName);
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (
            value.Contains(",")
            || value.Contains("\"")
            || value.Contains("\n")
            || value.Contains("\r")
        )
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage(
                "/Account/Login",
                new { culture = CurrentCulture, area = "Identity" }
            );

        // Explicitly clear ModelState errors for properties not related to DeleteAccount
        foreach (var key in ModelState.Keys.ToList())
        {
            if (!key.StartsWith("DeleteAccount"))
            {
                ModelState.Remove(key);
            }
        }

        if (
            string.IsNullOrEmpty(DeleteAccount.Password)
            || string.IsNullOrEmpty(DeleteAccount.ConfirmPhrase)
        )
        {
            ModelState.AddModelError(string.Empty, _localizer["DeleteAccountFieldsRequired"].Value);
            await LoadUserDataAsync(user);
            return Page();
        }

        if (DeleteAccount.ConfirmPhrase != "DELETE")
        {
            ModelState.AddModelError(
                string.Empty,
                _localizer["DeleteAccountConfirmPhraseMismatch"].Value
            );
            await LoadUserDataAsync(user);
            return Page();
        }

        if (!await _userManager.CheckPasswordAsync(user, DeleteAccount.Password))
        {
            ModelState.AddModelError("DeleteAccount.Password", _localizer["InvalidPassword"]);
        }

        if (!ModelState.IsValid)
        {
            await LoadUserDataAsync(user);
            return Page();
        }

        await _emailService.SendAccountDeletionConfirmationAsync(user.Email!);

        // Clean up data
        var drafts = await _context.Drafts.Where(d => d.UserId == user.Id).ToListAsync();
        _context.Drafts.RemoveRange(drafts);

        var submissions = await _context
            .SubmissionLogs.IgnoreQueryFilters()
            .Where(s => s.UserId == user.Id)
            .ToListAsync();
        foreach (var s in submissions)
            s.UserId = null;

        var candidates = await _context
            .CdrpCandidates.IgnoreQueryFilters()
            .Where(c => c.UserId == user.Id)
            .ToListAsync();
        foreach (var c in candidates)
            c.UserId = null;

        await _context.SaveChangesAsync();
        await _userManager.DeleteAsync(user);
        await _signInManager.SignOutAsync();

        return RedirectToPage("/Index", new { culture = CurrentCulture });
    }
}
