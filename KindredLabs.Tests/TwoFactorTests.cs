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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class TwoFactorTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Manage.TwoFactor>
    > _mockLocalizer;
    private readonly TwoFactorModel _model;

    public TwoFactorTests()
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

        _mockSecurityService = new Mock<ISecurityService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLocalizer =
            new Mock<IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Manage.TwoFactor>>();

        _model = new TwoFactorModel(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockSecurityService.Object,
            _mockEmailService.Object,
            _mockConfig.Object,
            _mockLocalizer.Object
        );

        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        _model.PageContext = new PageContext
        {
            HttpContext = httpContext,
            ActionDescriptor = new CompiledPageActionDescriptor(),
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
        };
        _model.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task OnPostEnableAsync_ValidCode_Enables2FA()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager
            .Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUserManager
            .Setup(m => m.SetTwoFactorEnabledAsync(user, true))
            .ReturnsAsync(IdentityResult.Success);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _model.Code = "123456";

        // Act
        var result = await _model.OnPostEnableAsync();

        // Assert
        _mockUserManager.Verify(m => m.SetTwoFactorEnabledAsync(user, true), Times.Once);
        _mockSecurityService.Verify(
            s =>
                s.RecordAuditEventAsync(
                    user.Id,
                    SecurityEventTypes.TwoFactorEnabled,
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
        _mockEmailService.Verify(
            e => e.SendTwoFactorEnabledNotificationAsync(user.Email, It.IsAny<string>()),
            Times.Once
        );
        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnPostDisableAsync_ClearsAuthenticatorAndCodes()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager
            .Setup(m => m.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(m => m.ResetAuthenticatorKeyAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostDisableAsync();

        // Assert
        _mockUserManager.Verify(m => m.SetTwoFactorEnabledAsync(user, false), Times.Once);
        _mockUserManager.Verify(m => m.ResetAuthenticatorKeyAsync(user), Times.Once);
        _mockSecurityService.Verify(
            s =>
                s.RecordAuditEventAsync(
                    user.Id,
                    SecurityEventTypes.TwoFactorDisabled,
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
        _mockEmailService.Verify(
            e => e.SendTwoFactorDisabledNotificationAsync(user.Email, It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public async Task OnGetAsync_StateLoadedCorrectly()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(_model.TwoFactorEnabled);
        Assert.Null(_model.AuthenticatorKey); // Should not load key if 2FA is already enabled
        Assert.Null(_model.QrCodeBase64);
    }

    [Fact]
    public async Task OnGetAsync_NotEnabled_LoadsKeyAndQrCode()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
        _mockUserManager
            .Setup(m => m.GetAuthenticatorKeyAsync(user))
            .ReturnsAsync("JBSWY3DPEHPK3PXP");

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        await _model.OnGetAsync();

        // Assert
        Assert.False(_model.TwoFactorEnabled);
        Assert.Equal("jbsw y3dp ehpk 3pxp", _model.AuthenticatorKey);
        Assert.NotNull(_model.QrCodeBase64);
        Assert.NotEmpty(_model.QrCodeBase64);
    }

    [Fact]
    public async Task OnGetAsync_NoKey_ResetsKey()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
        _mockUserManager
            .SetupSequence(m => m.GetAuthenticatorKeyAsync(user))
            .ReturnsAsync((string?)null)
            .ReturnsAsync("JBSWY3DPEHPK3PXP");
        _mockUserManager
            .Setup(m => m.ResetAuthenticatorKeyAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        await _model.OnGetAsync();

        // Assert
        _mockUserManager.Verify(m => m.ResetAuthenticatorKeyAsync(user), Times.Once);
        Assert.Equal("jbsw y3dp ehpk 3pxp", _model.AuthenticatorKey);
    }

    [Fact]
    public async Task OnPostEnableAsync_InvalidCode_ReturnsPageWithError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager
            .Setup(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockUserManager
            .Setup(m => m.GetAuthenticatorKeyAsync(user))
            .ReturnsAsync("JBSWY3DPEHPK3PXP");

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _model.Code = "000000";

        // Act
        var result = await _model.OnPostEnableAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        Assert.False(_model.TwoFactorEnabled);
        _mockUserManager.Verify(m => m.SetTwoFactorEnabledAsync(user, true), Times.Never);
    }

    [Fact]
    public async Task OnPostDisableAsync_IdentityFailure_ReturnsPageWithError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockUserManager
            .Setup(m => m.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostDisableAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        Assert.True(_model.TwoFactorEnabled);
    }
}
