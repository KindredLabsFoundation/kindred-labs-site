using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class LoginWithRecoveryCodeTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<ILogger<LoginWithRecoveryCodeModel>> _mockLogger;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.LoginWithRecoveryCode>
    > _mockLocalizer;
    private readonly LoginWithRecoveryCodeModel _model;

    public LoginWithRecoveryCodeTests()
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
        var httpContext = new DefaultHttpContext();
        // Mock IRequestCultureFeature
        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        httpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);
        httpContext.Request.Headers["User-Agent"] = "TestAgent";
        contextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
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
        _mockLogger = new Mock<ILogger<LoginWithRecoveryCodeModel>>();
        _mockLocalizer =
            new Mock<
                IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.LoginWithRecoveryCode>
            >();

        _model = new LoginWithRecoveryCodeModel(
            _mockSignInManager.Object,
            _mockUserManager.Object,
            _mockSecurityService.Object,
            _mockLogger.Object,
            _mockLocalizer.Object
        );

        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        _model.PageContext = new PageContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task OnPostAsync_ValidCode_SignsInSuccessfully()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);

        // Match what's sent in OnPostAsync: Input.RecoveryCode.Replace(" ", string.Empty).ToUpperInvariant()
        // If input is " CODE-1 ", it becomes "CODE-1"
        _mockSignInManager
            .Setup(s => s.TwoFactorRecoveryCodeSignInAsync("CODE-1"))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _model.Input.RecoveryCode = " CODE-1 ";

        _mockUserManager
            .Setup(m => m.GetValidTwoFactorProvidersAsync(user))
            .ReturnsAsync(new List<string> { "Authenticator" });

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        _mockSignInManager.Verify(s => s.TwoFactorRecoveryCodeSignInAsync("CODE-1"), Times.Once);
        Assert.IsType<LocalRedirectResult>(result);
        _mockSecurityService.Verify(
            s => s.RecordLoginAttemptAsync(user.Id, true, It.IsAny<string>(), "TestAgent"),
            Times.Once
        );
        _mockSecurityService.Verify(
            s =>
                s.RecordAuditEventAsync(
                    user.Id,
                    SecurityEventTypes.RecoveryCodeUsed,
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_ReturnsError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        _mockSignInManager
            .Setup(s => s.TwoFactorRecoveryCodeSignInAsync(It.IsAny<string>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        _model.Input.RecoveryCode = "WRONG";

        _mockUserManager
            .Setup(m => m.GetValidTwoFactorProvidersAsync(user))
            .ReturnsAsync(new List<string> { "Authenticator" });

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        _mockSecurityService.Verify(
            s => s.RecordLoginAttemptAsync(user.Id, false, It.IsAny<string>(), "TestAgent"),
            Times.Once
        );
    }

    [Fact]
    public async Task OnGetAsync_No2FASessionUser_RedirectsToLogin()
    {
        // Arrange
        _mockSignInManager
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _model.OnGetAsync("/");

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirectResult.PageName);
        Assert.Equal("en", redirectResult.RouteValues["culture"]);
    }

    [Fact]
    public async Task OnPostAsync_LockedOut_RedirectsToLockout()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        _mockSignInManager
            .Setup(s => s.TwoFactorRecoveryCodeSignInAsync(It.IsAny<string>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        _model.Input.RecoveryCode = "CODE-1";

        _mockUserManager
            .Setup(m => m.GetValidTwoFactorProvidersAsync(user))
            .ReturnsAsync(new List<string> { "Authenticator" });

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", redirectResult.PageName);
    }

    [Fact]
    public async Task OnPostAsync_LowercaseInput_ConvertedToUppercase()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        _mockSignInManager
            .Setup(s => s.TwoFactorRecoveryCodeSignInAsync("CODE-1"))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _model.Input.RecoveryCode = "code-1";

        _mockUserManager
            .Setup(m => m.GetValidTwoFactorProvidersAsync(user))
            .ReturnsAsync(new List<string> { "Authenticator" });

        // Act
        await _model.OnPostAsync("/");

        // Assert
        _mockSignInManager.Verify(s => s.TwoFactorRecoveryCodeSignInAsync("CODE-1"), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_InputWithSpaces_SpacesStripped()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        _mockSignInManager
            .Setup(s => s.TwoFactorRecoveryCodeSignInAsync("CODE1"))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _model.Input.RecoveryCode = "C O D E 1";

        _mockUserManager
            .Setup(m => m.GetValidTwoFactorProvidersAsync(user))
            .ReturnsAsync(new List<string> { "Authenticator" });

        // Act
        await _model.OnPostAsync("/");

        // Assert
        _mockSignInManager.Verify(s => s.TwoFactorRecoveryCodeSignInAsync("CODE1"), Times.Once);
    }
}
