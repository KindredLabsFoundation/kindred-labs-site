using System.Text;
using System.Text.Json;
using KindredLabs.Core.Constants;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Resources.Pages.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace KindredLabs.Web.Pages.Admin;

/// <summary>
/// Page model for the Admin Dashboard.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IAdminService _adminService;
    private readonly ICdrpCandidateService _cdrpCandidateService;
    private readonly IAdminRoleService _adminRoleService;
    private readonly ISecurityService _securityService;
    private readonly IEncryptionService _encryptionService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<Resources.Pages.Admin.Index> _localizer;

    public IStringLocalizer<Resources.Pages.Admin.Index> Localizer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    public IndexModel(
        IAdminService adminService,
        ICdrpCandidateService cdrpCandidateService,
        IAdminRoleService adminRoleService,
        ISecurityService securityService,
        IEncryptionService encryptionService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<Resources.Pages.Admin.Index> localizer
    )
    {
        _adminService = adminService;
        _cdrpCandidateService = cdrpCandidateService;
        _adminRoleService = adminRoleService;
        _securityService = securityService;
        _encryptionService = encryptionService;
        _context = context;
        _userManager = userManager;
        Localizer = localizer;
        _localizer = localizer;
    }

    public class CdrpCandidateViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public CandidateStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime? ResponseTokenExpiry { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? TermExpiresAt { get; set; }
        public bool RenewalRequested { get; set; }
        public DateTime? RetiredAt { get; set; }
        public DateTime? DeniedAt { get; set; }
    }

    public class SupplementaryEntry
    {
        public string Question { get; set; } = null!;
        public string? Response { get; set; }
    }

    public IEnumerable<ApplicationUser> Users { get; set; } = [];
    public IEnumerable<SubmissionLog> Submissions { get; set; } = [];
    public IEnumerable<CdrpCandidateViewModel> CdrpApplications { get; set; } = [];
    public IEnumerable<CdrpCandidateViewModel> CdrpActiveMembers { get; set; } = [];
    public IEnumerable<CdrpCandidateViewModel> CdrpRetiredMembers { get; set; } = [];
    public IEnumerable<CdrpCandidateViewModel> CdrpDeniedCandidates { get; set; } = [];
    public IEnumerable<AdminActivityLog> ActivityLogs { get; set; } = [];

    public bool IsOwner { get; set; }
    public string ActiveTab { get; set; } = "users";

    public ApplicationUser? SelectedUser { get; set; }
    public CdrpCandidate? SelectedCandidate { get; set; }
    public Dictionary<string, string> CandidateFields { get; set; } = [];
    public Dictionary<string, SupplementaryEntry> SupplementaryData { get; set; } = [];

    public int SubmissionCount { get; set; }
    public IEnumerable<SecurityAuditLog> UserSecurityLogs { get; set; } = [];
    public IEnumerable<LoginHistory> UserLoginHistory { get; set; } = [];

    /// <summary>
    /// Handles GET requests to the admin dashboard.
    /// </summary>
    private async Task<IActionResult?> CheckAccessAsync(ApplicationUser admin)
    {
        IsOwner = await _userManager.IsInRoleAsync(admin, AppRoles.OwnerRole);
        var isAdmin = await _userManager.IsInRoleAsync(admin, AppRoles.AdminRole);

        if (!IsOwner && !isAdmin)
        {
            return NotFound();
        }

        if (!IsOwner && !admin.TwoFactorEnabled)
        {
            var culture = HttpContext.Items["culture"]?.ToString() ?? "en";
            TempData["StatusMessage"] = "Error: " + _localizer["TwoFactorRequired"];
            return RedirectToPage("/Account/Manage/TwoFactor", new { culture });
        }

        return null;
    }

    public async Task<IActionResult> OnGetAsync(string? tab, string? userId, Guid? candidateId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return NotFound();

        var accessCheck = await CheckAccessAsync(currentUser);
        if (accessCheck != null)
            return accessCheck;

        ActiveTab = tab ?? "users";
        await LoadDashboardDataAsync(currentUser.Id);

        if (!string.IsNullOrEmpty(userId))
        {
            await LoadUserDetailsAsync(userId);
        }

        if (candidateId.HasValue)
        {
            await LoadCandidateDetailsAsync(candidateId.Value);
        }

        return Page();
    }

    private async Task LoadDashboardDataAsync(string currentUserId)
    {
        Users = await _adminService.GetAllUsersAsync();

        Submissions = await _context
            .SubmissionLogs.Include(l => l.User)
            .OrderByDescending(l => l.SubmittedAt)
            .Take(100)
            .ToListAsync();

        var candidates = await _cdrpCandidateService.GetAllCandidatesAsync();
        var candidateViewModels = candidates
            .Select(c =>
            {
                // FormData is already decrypted by GetAllCandidatesAsync
                var formData =
                    JsonSerializer.Deserialize<Dictionary<string, object>>(c.FormData)
                    ?? new Dictionary<string, object>();
                var name = formData.TryGetValue("Name", out var n) ? n.ToString() : "Unknown";

                return new CdrpCandidateViewModel
                {
                    Id = c.Id,
                    Name = name!,
                    Email = c.Email,
                    Status = c.Status,
                    SubmittedAt = c.SubmittedAt,
                    AdminNotes = c.AdminNotes,
                    ResponseTokenExpiry = c.ResponseTokenExpiry,
                    ApprovedAt = c.ApprovedAt,
                    TermExpiresAt = c.TermExpiresAt,
                    RenewalRequested = c.RenewalRequested,
                    RetiredAt = c.RetiredAt,
                    DeniedAt = c.DeniedAt,
                };
            })
            .ToList();

        CdrpApplications = candidateViewModels.Where(c =>
            c.Status == CandidateStatus.Pending
            || c.Status == CandidateStatus.UnderReview
            || c.Status == CandidateStatus.AwaitingResponse
        );

        CdrpActiveMembers = candidateViewModels.Where(c =>
            c.Status == CandidateStatus.Active || c.Status == CandidateStatus.Approved
        );
        CdrpRetiredMembers = candidateViewModels.Where(c => c.Status == CandidateStatus.Retired);
        CdrpDeniedCandidates = candidateViewModels.Where(c => c.Status == CandidateStatus.Denied);

        if (IsOwner)
        {
            ActivityLogs = await _adminService.GetAdminActivityLogAsync(maxRecords: 100);
        }
        else
        {
            ActivityLogs = await _adminService.GetAdminActivityLogAsync(
                adminUserId: currentUserId,
                maxRecords: 100
            );
        }
    }

    private async Task LoadCandidateDetailsAsync(Guid candidateId)
    {
        SelectedCandidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (SelectedCandidate != null)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin != null)
            {
                await _adminService.LogAdminActionAsync(
                    admin.Id,
                    SelectedCandidate.UserId,
                    SecurityEventTypes.CdrpApplicationViewed,
                    $"Viewed CDRP application for {SelectedCandidate.Email} ({SelectedCandidate.Id})",
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );
            }

            // SelectedCandidate.FormData is already decrypted by GetCandidateByIdAsync
            var rawFields = JsonSerializer.Deserialize<Dictionary<string, object>>(
                SelectedCandidate.FormData
            );
            CandidateFields =
                rawFields
                    ?.Where(kvp => kvp.Key != "Acknowledgment")
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty)
                ?? new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(SelectedCandidate.SupplementaryData))
            {
                SupplementaryData =
                    JsonSerializer.Deserialize<Dictionary<string, SupplementaryEntry>>(
                        SelectedCandidate.SupplementaryData
                    ) ?? new Dictionary<string, SupplementaryEntry>();
            }

            if (SelectedCandidate.Status == CandidateStatus.Pending)
            {
                await _cdrpCandidateService.SetStatusAsync(
                    candidateId,
                    CandidateStatus.UnderReview
                );
                SelectedCandidate.Status = CandidateStatus.UnderReview;
            }
        }
    }

    private async Task LoadUserDetailsAsync(string userId)
    {
        SelectedUser = await _adminService.GetUserByIdAsync(userId);
        if (SelectedUser != null)
        {
            SubmissionCount = await _context.SubmissionLogs.CountAsync(l => l.UserId == userId);
            UserSecurityLogs = await _context
                .SecurityAuditLogs.Where(l => l.UserId == userId)
                .OrderByDescending(l => l.PerformedAt)
                .Take(10)
                .ToListAsync();
            UserLoginHistory = await _context
                .LoginHistories.Where(l => l.UserId == userId)
                .OrderByDescending(l => l.LoginAt)
                .Take(10)
                .ToListAsync();
        }
    }

    public async Task<IActionResult> OnPostSuspendUserAsync(string userId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        await _adminService.SuspendUserAsync(
            admin.Id,
            userId,
            "Suspended via Admin Dashboard",
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        return RedirectToPage(new { tab = "users" });
    }

    public async Task<IActionResult> OnPostUnsuspendUserAsync(string userId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        await _adminService.UnsuspendUserAsync(
            admin.Id,
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        return RedirectToPage(new { tab = "users" });
    }

    public async Task<IActionResult> OnPostResetTwoFactorAsync(string userId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        await _adminService.ResetTwoFactorAsync(
            admin.Id,
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        return RedirectToPage(new { tab = "users" });
    }

    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        await _adminService.DeleteUserAsync(
            admin.Id,
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        return RedirectToPage(new { tab = "users" });
    }

    public async Task<IActionResult> OnPostAssignAdminAsync(string userId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;
        if (!IsOwner)
            return NotFound();

        await _adminService.AssignAdminRoleAsync(
            admin.Id,
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        return RedirectToPage(new { tab = "users" });
    }

    public async Task<IActionResult> OnPostRevokeAdminAsync(string userId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;
        if (!IsOwner)
            return NotFound();

        await _adminService.RevokeAdminRoleAsync(
            admin.Id,
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        return RedirectToPage(new { tab = "users" });
    }

    public async Task<IActionResult> OnPostRestoreUserAsync(string userId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;
        if (!IsOwner)
            return NotFound();

        await _adminService.RestoreUserAsync(
            admin.Id,
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        return RedirectToPage(new { tab = "users" });
    }

    public async Task<IActionResult> OnPostUpdateCdrpStatusAsync(Guid candidateId, int status)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        await _cdrpCandidateService.UpdateCandidateStatusAsync(
            candidateId,
            (CandidateStatus)status,
            null
        );
        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostDeleteCandidateAsync(Guid candidateId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var candidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (candidate != null)
        {
            await _cdrpCandidateService.DeleteCandidateAsync(candidateId);
            await _adminService.LogAdminActionAsync(
                admin.Id,
                candidate.UserId,
                SecurityEventTypes.CdrpApplicationDeleted,
                $"Deleted CDRP application for {candidate.Email} ({candidateId})",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostResendQuestionsAsync(Guid candidateId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var currentCulture = System
            .Globalization
            .CultureInfo
            .CurrentCulture
            .TwoLetterISOLanguageName;
        var respondUrl = $"{Request.Scheme}://{Request.Host}/{currentCulture}/CDRP/Respond";

        await _cdrpCandidateService.ResendQuestionsAsync(candidateId, respondUrl);

        var candidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (candidate != null)
        {
            await _adminService.LogAdminActionAsync(
                admin.Id,
                candidate.UserId,
                SecurityEventTypes.CdrpSupplementaryRequestSent,
                $"Resent supplementary questions for CDRP application for {candidate.Email} ({candidateId})",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostSendNewLinkAsync(Guid candidateId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var currentCulture = System
            .Globalization
            .CultureInfo
            .CurrentCulture
            .TwoLetterISOLanguageName;
        var respondUrl = $"{Request.Scheme}://{Request.Host}/{currentCulture}/CDRP/Respond";

        await _cdrpCandidateService.SendNewLinkAsync(candidateId, respondUrl);

        var candidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (candidate != null)
        {
            await _adminService.LogAdminActionAsync(
                admin.Id,
                candidate.UserId,
                SecurityEventTypes.CdrpSupplementaryRequestSent,
                $"Sent new response link for CDRP application for {candidate.Email} ({candidateId})",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostApproveCandidateAsync(Guid candidateId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var candidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (candidate != null)
        {
            await _cdrpCandidateService.ApproveCandidateAsync(candidateId);
            await _adminService.LogAdminActionAsync(
                admin.Id,
                candidate.UserId,
                SecurityEventTypes.CdrpApplicationApproved,
                $"Approved CDRP application for {candidate.Email} ({candidateId})",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostDenyCandidateAsync(Guid candidateId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var candidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (candidate != null)
        {
            await _cdrpCandidateService.DenyCandidateAsync(candidateId);
            await _adminService.LogAdminActionAsync(
                admin.Id,
                candidate.UserId,
                SecurityEventTypes.CdrpApplicationDenied,
                $"Denied CDRP application for {candidate.Email} ({candidateId})",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostRetireCandidateAsync(Guid candidateId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var candidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (candidate != null)
        {
            await _cdrpCandidateService.RetireCandidateAsync(candidateId);
            await _adminService.LogAdminActionAsync(
                admin.Id,
                candidate.UserId,
                SecurityEventTypes.CdrpMemberRetired,
                $"Retired CDRP member {candidate.Email} ({candidateId})",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostRenewTermAsync(Guid candidateId)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var candidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (candidate != null)
        {
            await _cdrpCandidateService.RenewTermAsync(candidateId);
            await _adminService.LogAdminActionAsync(
                admin.Id,
                candidate.UserId,
                SecurityEventTypes.CdrpTermRenewed,
                $"Renewed CDRP term for {candidate.Email} ({candidateId})",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostSendSupplementaryRequestAsync(
        Guid candidateId,
        Dictionary<string, string> questions
    )
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var currentCulture = System
            .Globalization
            .CultureInfo
            .CurrentCulture
            .TwoLetterISOLanguageName;
        var respondUrl = $"{Request.Scheme}://{Request.Host}/{currentCulture}/CDRP/Respond";
        await _cdrpCandidateService.SendSupplementaryRequestAsync(
            candidateId,
            questions,
            respondUrl
        );

        var candidate = await _cdrpCandidateService.GetCandidateByIdAsync(candidateId);
        if (candidate != null)
        {
            await _adminService.LogAdminActionAsync(
                admin.Id,
                candidate.UserId,
                SecurityEventTypes.CdrpSupplementaryRequestSent,
                $"Sent supplementary questions for CDRP application for {candidate.Email} ({candidate.Id})",
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnPostUpdateCdrpNotesAsync(Guid candidateId, string notes)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        var candidate = (await _cdrpCandidateService.GetAllCandidatesAsync()).FirstOrDefault(c =>
            c.Id == candidateId
        );
        if (candidate != null)
        {
            await _cdrpCandidateService.UpdateCandidateStatusAsync(
                candidateId,
                candidate.Status,
                notes
            );
        }
        return RedirectToPage(new { tab = "cdrp" });
    }

    public async Task<IActionResult> OnGetDownloadSubmissionsAsync(string format)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        if (format != "csv")
            return BadRequest();

        var submissions = await _context
            .SubmissionLogs.Include(l => l.User)
            .OrderByDescending(l => l.SubmittedAt)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Id,FormType,SubmittedAt,Hash,UserEmail");
        foreach (var s in submissions)
        {
            csv.AppendLine(
                $"{s.Id},{s.FormType},{s.SubmittedAt:O},{s.ContentHash},{s.User?.Email ?? "Anonymous"}"
            );
        }

        return File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            $"submissions_{DateTime.UtcNow:yyyyMMdd}.csv"
        );
    }

    public async Task<IActionResult> OnGetDownloadActivityLogAsync(string format)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return NotFound();
        var accessCheck = await CheckAccessAsync(admin);
        if (accessCheck != null)
            return accessCheck;

        string? adminId = IsOwner ? null : admin.Id;

        string content;
        string contentType;
        string fileName;

        if (format == "csv")
        {
            content = await _adminService.ExportAdminActivityLogAsCsvAsync(adminId);
            contentType = "text/csv";
            fileName = $"admin_activity_{DateTime.UtcNow:yyyyMMdd}.csv";
        }
        else if (format == "txt")
        {
            content = await _adminService.ExportAdminActivityLogAsTxtAsync(adminId);
            contentType = "text/plain";
            fileName = $"admin_activity_{DateTime.UtcNow:yyyyMMdd}.txt";
        }
        else
        {
            return BadRequest();
        }

        return File(Encoding.UTF8.GetBytes(content), contentType, fileName);
    }
}
