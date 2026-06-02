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
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class LoginWith2faTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<ILogger<LoginWith2faModel>> _mockLogger;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.LoginWith2fa>
    > _mockLocalizer;
    private readonly LoginWith2faModel _model;

    public LoginWith2faTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        );

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null,
            null,
            null,
            null
        );

        _mockSecurityService = new Mock<ISecurityService>();
        _mockLogger = new Mock<ILogger<LoginWith2faModel>>();
        _mockLocalizer =
            new Mock<IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.LoginWith2fa>>();

        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        _model = new LoginWith2faModel(
            _mockSignInManager.Object,
            _mockUserManager.Object,
            _mockSecurityService.Object,
            _mockLogger.Object,
            _mockLocalizer.Object
        );

        var httpContext = new DefaultHttpContext();
        _model.PageContext = new PageContext
        {
            HttpContext = httpContext,
            ActionDescriptor = new CompiledPageActionDescriptor(),
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
        };
        _model.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(x => x.Content(It.IsAny<string>()))
            .Returns((string content) => content);
        _model.Url = urlHelperMock.Object;
    }

    [Fact]
    public async Task OnGetAsync_Missing2FASession_RedirectsToLogin()
    {
        // Arrange
        _mockSignInManager
            .Setup(s => s.GetTwoFactorAuthenticationUserAsync())
            .ReturnsAsync((ApplicationUser?)null);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnGetAsync(false);

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirectResult.PageName);
    }

    [Fact]
    public async Task OnPostAsync_ValidCode_SignsInSuccessfully()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        _mockSignInManager
            .Setup(s =>
                s.TwoFactorAuthenticatorSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()
                )
            )
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _model.Input.TwoFactorCode = "123456";

        _model.HttpContext.Request.Headers["User-Agent"] = "TestAgent";

        // Act
        var result = await _model.OnPostAsync(false, "/");

        // Assert
        Assert.IsType<LocalRedirectResult>(result);
        _mockSecurityService.Verify(
            s => s.RecordLoginAttemptAsync(user.Id, true, It.IsAny<string>(), "TestAgent"),
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
            .Setup(s =>
                s.TwoFactorAuthenticatorSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()
                )
            )
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        _model.Input.TwoFactorCode = "000000";

        _model.HttpContext.Request.Headers["User-Agent"] = "TestAgent";

        // Act
        var result = await _model.OnPostAsync(false, "/");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        _mockSecurityService.Verify(
            s => s.RecordLoginAttemptAsync(user.Id, false, It.IsAny<string>(), "TestAgent"),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPostAsync_LockedOut_RedirectsToLockout()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        _mockSignInManager
            .Setup(s =>
                s.TwoFactorAuthenticatorSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()
                )
            )
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        _model.Input.TwoFactorCode = "123456";

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("es"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostAsync(false, "/");

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", redirectResult.PageName);
        Assert.Equal("es", redirectResult.RouteValues["culture"]);
    }

    [Fact]
    public async Task OnPostAsync_CodeWithSpaces_StripsSpacesAndSucceeds()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        _mockSignInManager
            .Setup(s =>
                s.TwoFactorAuthenticatorSignInAsync("123456", It.IsAny<bool>(), It.IsAny<bool>())
            )
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _model.Input.TwoFactorCode = "123 456";

        // Act
        await _model.OnPostAsync(false, "/");

        // Assert
        _mockSignInManager.Verify(
            s => s.TwoFactorAuthenticatorSignInAsync("123456", It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPostAsync_MissingUserAgent_CallsRecordLoginAttemptWithNull()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockSignInManager.Setup(s => s.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        _mockSignInManager
            .Setup(s =>
                s.TwoFactorAuthenticatorSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()
                )
            )
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _model.Input.TwoFactorCode = "123456";

        _model.HttpContext.Request.Headers.Remove("User-Agent");

        // Act
        await _model.OnPostAsync(false, "/");

        // Assert
        _mockSecurityService.Verify(
            s => s.RecordLoginAttemptAsync(user.Id, true, It.IsAny<string>(), ""),
            Times.Once
        );
    }
}
