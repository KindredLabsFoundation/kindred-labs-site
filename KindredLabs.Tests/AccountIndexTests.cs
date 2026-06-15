using System.Security.Claims;
using KindredLabs.Core.Data;
using KindredLabs.Core.Models.CDRP;
using KindredLabs.Core.Models.Forms;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class AccountIndexTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Index>
    > _mockLocalizer;
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<ILogger<IndexModel>> _mockLogger;
    private readonly IndexModel _model;

    public AccountIndexTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!
        );

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

        var httpContext = new DefaultHttpContext();
        contextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!,
            null!,
            null!,
            null!
        );

        _mockEmailService = new Mock<IEmailService>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLocalizer =
            new Mock<IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Index>>();
        _mockSecurityService = new Mock<ISecurityService>();
        _mockLogger = new Mock<ILogger<IndexModel>>();

        _model = new IndexModel(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _context,
            _mockEmailService.Object,
            _mockConfig.Object,
            _mockLocalizer.Object,
            _mockSecurityService.Object,
            _mockLogger.Object
        );

        // Setup default localizer behavior
        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        // Setup PageContext
        _model.PageContext = new PageContext
        {
            HttpContext = httpContext,
            ActionDescriptor = new CompiledPageActionDescriptor(),
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
        };
        _model.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        // Setup UrlHelper
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("callback");
        urlHelperMock.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("callback");
        urlHelperMock
            .Setup(x => x.Content(It.IsAny<string>()))
            .Returns((string content) => content);

        var actionContext = new ActionContext(
            httpContext,
            _model.PageContext.RouteData,
            _model.PageContext.ActionDescriptor
        );
        urlHelperMock.SetupGet(x => x.ActionContext).Returns(actionContext);

        _model.Url = urlHelperMock.Object;
    }

    [Fact]
    public async Task OnPostSaveProfileAsync_PersistsAllFields()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _model.Profile = new IndexModel.ProfileInput
        {
            FirstName = "John",
            LastName = "Doe",
            Organization = "Kindred",
            JobTitle = "Tester",
        };

        // Act
        var result = await _model.OnPostSaveProfileAsync();

        // Assert
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("Kindred", user.Organization);
        Assert.Equal("Tester", user.JobTitle);
        _mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostAddEmailAsync_CreatesUnverifiedRecord_SendsConfirmation()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "primary@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);
        _model.HttpContext.Request.Scheme = "https";

        _model.NewEmail = "secondary@test.com";

        // Act
        await _model.OnPostAddEmailAsync();

        // Assert
        var record = await _context.UserEmails.FirstOrDefaultAsync(e =>
            e.Email == "secondary@test.com"
        );
        Assert.NotNull(record);
        Assert.False(record.IsVerified);
        Assert.NotNull(record.VerificationToken);
        _mockEmailService.Verify(
            e => e.SendAdditionalEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public async Task OnGetConfirmAdditionalEmailAsync_VerifiesEmail()
    {
        // Arrange
        var token = Guid.NewGuid().ToString();
        var emailRecord = new UserEmail
        {
            UserId = "user1",
            Email = "test@test.com",
            VerificationToken = token,
            VerificationTokenExpiry = DateTime.UtcNow.AddHours(1),
        };
        _context.UserEmails.Add(emailRecord);
        await _context.SaveChangesAsync();

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        await _model.OnGetConfirmAdditionalEmailAsync(token);

        // Assert
        Assert.True(emailRecord.IsVerified);
        Assert.Null(emailRecord.VerificationToken);
    }

    [Fact]
    public async Task OnPostDeleteAccountAsync_DeletesUserData()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);
        _mockUserManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _model.DeleteAccount = new IndexModel.DeleteAccountInput
        {
            Password = "Password123!",
            ConfirmPhrase = "DELETE",
        };

        _context.Drafts.Add(
            new Draft
            {
                UserId = "user1",
                FormType = FormType.DataProvenance,
                FormData = "{}",
            }
        );
        _context.SubmissionLogs.Add(new SubmissionLog { UserId = "user1", ContentHash = "hash" });
        _context.CdrpCandidates.Add(new CdrpCandidate { UserId = "user1", FormData = "data", Email = "test@example.com" });
        await _context.SaveChangesAsync();

        // Act
        await _model.OnPostDeleteAccountAsync();

        // Assert
        Assert.Empty(await _context.Drafts.Where(d => d.UserId == "user1").ToListAsync());
        var log = await _context.SubmissionLogs.FirstAsync();
        Assert.Null(log.UserId);
        var candidate = await _context.CdrpCandidates.FirstAsync();
        Assert.Null(candidate.UserId);
        _mockEmailService.Verify(
            e => e.SendAccountDeletionConfirmationAsync(user.Email),
            Times.Once
        );
        _mockUserManager.Verify(m => m.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task OnPostSavePreferencesAsync_ValidCulture_UpdatesUserAndSetsCookie()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", PreferredLocale = "en" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _model.PreferredLanguage = "es";

        // Act
        var result = await _model.OnPostSavePreferencesAsync();

        // Assert
        Assert.Equal("es", user.PreferredLocale);
        _mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);
        Assert.True(_model.Response.Headers.ContainsKey("Set-Cookie"));
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("es", redirectResult.RouteValues["culture"]);
    }

    [Fact]
    public async Task OnPostSavePreferencesAsync_UpdateFailed_ReturnsError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", PreferredLocale = "en" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed());

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _model.PreferredLanguage = "es";

        // Act
        await _model.OnPostSavePreferencesAsync();

        // Assert
        Assert.Equal("PreferencesUpdateFailed", _model.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task OnPostMakePrimaryAsync_VerifiedEmail_UpdatesPrimaryAndSignsOut()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "old@test.com",
            UserName = "old@test.com",
        };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var emailId = Guid.NewGuid();
        var newEmailRecord = new UserEmail
        {
            Id = emailId,
            UserId = user.Id,
            Email = "new@test.com",
            IsVerified = true,
            IsPrimary = false,
        };
        var oldEmailRecord = new UserEmail
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = "old@test.com",
            IsVerified = true,
            IsPrimary = true,
        };
        _context.UserEmails.AddRange(newEmailRecord, oldEmailRecord);
        await _context.SaveChangesAsync();

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostMakePrimaryAsync(emailId);

        // Assert
        Assert.Equal("new@test.com", user.Email);
        Assert.Equal("new@test.com", user.UserName);
        Assert.True(newEmailRecord.IsPrimary);
        Assert.False(oldEmailRecord.IsPrimary);

        _mockEmailService.Verify(
            e =>
                e.SendPrimaryEmailChangedNotificationAsync(
                    "old@test.com",
                    "new@test.com",
                    It.IsAny<string>()
                ),
            Times.Once
        );
        _mockUserManager.Verify(m => m.UpdateSecurityStampAsync(user), Times.Once);
        _mockSignInManager.Verify(s => s.SignOutAsync(), Times.Once);

        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Login", redirectResult.PageName);
    }

    [Fact]
    public async Task OnPostMakePrimaryAsync_EmailNotFound_RedirectsToPage()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostMakePrimaryAsync(Guid.NewGuid());

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveEmailAsync_NonPrimary_DeletesEmail()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var emailId = Guid.NewGuid();
        var emailRecord = new UserEmail
        {
            Id = emailId,
            UserId = user.Id,
            Email = "remove@test.com",
            IsPrimary = false,
        };
        _context.UserEmails.Add(emailRecord);
        await _context.SaveChangesAsync();

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        await _model.OnPostRemoveEmailAsync(emailId);

        // Assert
        Assert.Null(await _context.UserEmails.FindAsync(emailId));
        Assert.Equal("EmailRemovedSuccess", _model.TempData["StatusMessage"]);
    }

    [Fact]
    public async Task OnPostRemoveEmailAsync_PrimaryEmail_DoesNotDelete()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var emailId = Guid.NewGuid();
        var emailRecord = new UserEmail
        {
            Id = emailId,
            UserId = user.Id,
            Email = "primary@test.com",
            IsPrimary = true,
        };
        _context.UserEmails.Add(emailRecord);
        await _context.SaveChangesAsync();

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        await _model.OnPostRemoveEmailAsync(emailId);

        // Assert
        Assert.NotNull(await _context.UserEmails.FindAsync(emailId));
    }

    [Fact]
    public async Task OnPostResendVerificationAsync_ValidEmail_SendsEmail()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var emailId = Guid.NewGuid();
        var emailRecord = new UserEmail
        {
            Id = emailId,
            UserId = user.Id,
            Email = "unverified@test.com",
            IsVerified = false,
        };
        _context.UserEmails.Add(emailRecord);
        await _context.SaveChangesAsync();

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        await _model.OnPostResendVerificationAsync(emailId);

        // Assert
        _mockEmailService.Verify(
            e => e.SendAdditionalEmailConfirmationAsync(emailRecord.Email, It.IsAny<string>()),
            Times.Once
        );
        Assert.Equal("VerificationResent", _model.TempData["StatusMessage"]);
    }

    [Fact]
    public async Task OnPostRevokeOtherSessionsAsync_UpdatesSecurityStampAndRefreshesSignIn()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        await _model.OnPostRevokeOtherSessionsAsync();

        // Assert
        _mockUserManager.Verify(m => m.UpdateSecurityStampAsync(user), Times.Once);
        _mockSignInManager.Verify(s => s.RefreshSignInAsync(user), Times.Once);
        _mockSecurityService.Verify(
            s =>
                s.RecordAuditEventAsync(
                    user.Id,
                    SecurityEventTypes.OtherSessionsRevoked,
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
        Assert.Equal("OtherSessionsRevokedSuccess", _model.TempData["StatusMessage"]);
    }

    [Fact]
    public async Task OnGetDownloadDataAsync_CsvFormat_ReturnsCsvFile()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockSecurityService
            .Setup(s => s.GetLoginHistoryAsync(user.Id, It.IsAny<int>()))
            .ReturnsAsync(new List<LoginHistory>());

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnGetDownloadDataAsync("csv");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains(
            "SECTION: PROFILE",
            System.Text.Encoding.UTF8.GetString(fileResult.FileContents)
        );
    }

    [Fact]
    public async Task OnGetDownloadDataAsync_TxtFormat_ReturnsTxtFile()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockSecurityService
            .Setup(s => s.GetLoginHistoryAsync(user.Id, It.IsAny<int>()))
            .ReturnsAsync(new List<LoginHistory>());

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnGetDownloadDataAsync("txt");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
    }

    [Fact]
    public async Task OnGetConfirmAdditionalEmailAsync_ExpiredToken_ReturnsError()
    {
        // Arrange
        var token = "expired-token";
        var emailRecord = new UserEmail
        {
            UserId = "user1",
            Email = "expired@test.com",
            VerificationToken = token,
            VerificationTokenExpiry = DateTime.UtcNow.AddHours(-1),
        };
        _context.UserEmails.Add(emailRecord);
        await _context.SaveChangesAsync();

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        await _model.OnGetConfirmAdditionalEmailAsync(token);

        // Assert
        Assert.Equal("EmailConfirmationFailed", _model.TempData["ErrorMessage"]);
        Assert.False(emailRecord.IsVerified);
    }

    [Fact]
    public async Task OnPostMakePrimaryAsync_UnverifiedEmail_DoesNotMakePrimary()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "old@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var emailId = Guid.NewGuid();
        var unverifiedEmail = new UserEmail
        {
            Id = emailId,
            UserId = user.Id,
            Email = "new@test.com",
            IsVerified = false,
            IsPrimary = false,
        };
        _context.UserEmails.Add(unverifiedEmail);
        await _context.SaveChangesAsync();

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostMakePrimaryAsync(emailId);

        // Assert
        Assert.Equal("old@test.com", user.Email);
        Assert.False(unverifiedEmail.IsPrimary);
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostResendVerificationAsync_EmailNotFound_RedirectsToPage()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostResendVerificationAsync(Guid.NewGuid());

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
