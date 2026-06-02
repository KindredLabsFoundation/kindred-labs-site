using System.Security.Claims;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class RecoveryCodesTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<ILogger<RecoveryCodesModel>> _mockLogger;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Manage.RecoveryCodes>
    > _mockLocalizer;
    private readonly RecoveryCodesModel _model;

    public RecoveryCodesTests()
    {
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
        _mockSecurityService = new Mock<ISecurityService>();
        _mockLogger = new Mock<ILogger<RecoveryCodesModel>>();
        _mockLocalizer =
            new Mock<
                IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Manage.RecoveryCodes>
            >();

        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        _model = new RecoveryCodesModel(
            _mockUserManager.Object,
            _mockSecurityService.Object,
            _mockLogger.Object,
            _mockLocalizer.Object
        );

        var httpContext = new DefaultHttpContext();
        // Mock IRequestCultureFeature
        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        httpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _model.PageContext = new PageContext { HttpContext = httpContext };
        _model.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task OnPostGenerateAsync_Requires2FAEnabled()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);

        // Act
        var result = await _model.OnPostGenerateAsync();

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("2FA not enabled")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
            Times.Once
        );
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostGenerateAsync_PersistsCodesAndRecordsAudit()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        var codes = new List<string> { "CODE1-CODE2", "CODE3-CODE4" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
        _mockUserManager
            .Setup(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
            .ReturnsAsync(codes);

        // Act
        await _model.OnPostGenerateAsync();

        // Assert
        Assert.Equal(codes, _model.GeneratedCodes);
        _mockSecurityService.Verify(
            s =>
                s.RecordAuditEventAsync(
                    user.Id,
                    SecurityEventTypes.RecoveryCodesGenerated,
                    null,
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task OnGetAsync_StateLoadedCorrectly()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
        _mockUserManager.Setup(m => m.CountRecoveryCodesAsync(user)).ReturnsAsync(5);

        // Act
        var result = await _model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(5, _model.RecoveryCodeCount);
    }

    [Fact]
    public async Task OnGetAsync_With2FADisabled_Redirects()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);

        // Act
        var result = await _model.OnGetAsync();

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Manage/TwoFactor", redirectResult.PageName);
    }

    [Fact]
    public async Task OnPostGenerateAsync_With2FADisabled_ReturnsPageWithError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
        _mockUserManager.Setup(m => m.CountRecoveryCodesAsync(user)).ReturnsAsync(0);

        // Act
        var result = await _model.OnPostGenerateAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        _mockUserManager.Verify(
            m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10),
            Times.Never
        );
    }

    [Fact]
    public async Task OnPostGenerateAsync_WhenGenerationReturnsNull_ReturnsPageWithError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
        _mockUserManager
            .Setup(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
            .ReturnsAsync((IEnumerable<string>?)null);
        _mockUserManager.Setup(m => m.CountRecoveryCodesAsync(user)).ReturnsAsync(0);

        // Act
        var result = await _model.OnPostGenerateAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
    }
}
