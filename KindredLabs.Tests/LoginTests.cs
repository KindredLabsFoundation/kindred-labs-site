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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class LoginTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<LoginModel>> _mockLogger;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Login>
    > _mockLocalizer;
    private readonly LoginModel _model;

    public LoginTests()
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
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<LoginModel>>();
        _mockLocalizer =
            new Mock<IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Login>>();

        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        _model = new LoginModel(
            _mockSignInManager.Object,
            _mockUserManager.Object,
            _mockSecurityService.Object,
            _mockConfiguration.Object,
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
    public async Task OnPostAsync_SuccessfulLogin_RecordsHistory()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@test.com",
            EmailConfirmed = true,
            PrivacyPolicyVersion = "1.0",
            TermsOfServiceVersion = "1.0",
        };
        _mockUserManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

        _mockSignInManager
            .Setup(s => s.PasswordSignInAsync("test@test.com", "Password123!", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _mockConfiguration.Setup(c => c["PolicyVersions:PrivacyPolicy"]).Returns("1.0");
        _mockConfiguration.Setup(c => c["PolicyVersions:TermsOfService"]).Returns("1.0");

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);
        _model.HttpContext.Request.Headers["User-Agent"] = "TestAgent";

        _model.Input = new LoginModel.InputModel
        {
            Email = "test@test.com",
            Password = "Password123!",
        };

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        _mockSecurityService.Verify(
            s => s.RecordLoginAttemptAsync(user.Id, true, It.IsAny<string>(), "TestAgent"),
            Times.Once
        );
        Assert.IsType<LocalRedirectResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_SuccessfulLogin_RedirectsToReviewPolicies_WhenVersionsMismatch()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@test.com",
            EmailConfirmed = true,
            PrivacyPolicyVersion = "0.9",
            TermsOfServiceVersion = "1.0",
        };
        _mockUserManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

        _mockSignInManager
            .Setup(s => s.PasswordSignInAsync("test@test.com", "Password123!", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _mockConfiguration.Setup(c => c["PolicyVersions:PrivacyPolicy"]).Returns("1.0");
        _mockConfiguration.Setup(c => c["PolicyVersions:TermsOfService"]).Returns("1.0");

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _model.Input = new LoginModel.InputModel
        {
            Email = "test@test.com",
            Password = "Password123!",
        };

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./ReviewPolicies", redirectResult.PageName);
    }

    [Fact]
    public async Task OnPostAsync_RequiresTwoFactor_RedirectsToLoginWith2fa()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "test@test.com",
            EmailConfirmed = true,
        };
        _mockUserManager.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);
        _model.HttpContext.Request.Headers["User-Agent"] = "TestAgent";

        var signInResult = Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired;
        _mockSignInManager
            .Setup(s => s.PasswordSignInAsync("test@test.com", "Password123!", true, false))
            .ReturnsAsync(signInResult);

        _model.Input = new LoginModel.InputModel
        {
            Email = "test@test.com",
            Password = "Password123!",
            RememberMe = true,
        };

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./LoginWith2fa", redirectResult.PageName);
        Assert.Equal(true, redirectResult.RouteValues["rememberMe"]);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCredentials_ReturnsPageWithError()
    {
        // Arrange
        _mockUserManager
            .Setup(m => m.FindByEmailAsync("wrong@test.com"))
            .ReturnsAsync((ApplicationUser?)null);
        _mockSignInManager
            .Setup(s => s.PasswordSignInAsync("wrong@test.com", "any", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        _model.Input = new LoginModel.InputModel { Email = "wrong@test.com", Password = "any" };

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        Assert.True(_model.ModelState.ContainsKey(string.Empty));
    }

    [Fact]
    public async Task OnPostAsync_LockoutResult_RedirectsToLockout()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", Email = "locked@test.com" };
        _mockUserManager.Setup(m => m.FindByEmailAsync("locked@test.com")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "any")).ReturnsAsync(true);

        _mockSignInManager
            .Setup(s => s.PasswordSignInAsync("locked@test.com", "any", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        _model.Input = new LoginModel.InputModel { Email = "locked@test.com", Password = "any" };

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        var redirectResult = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", redirectResult.PageName);
        Assert.Equal("en", redirectResult.RouteValues["culture"]);
    }

    [Fact]
    public async Task OnPostAsync_UnconfirmedEmail_FailsWithPageError()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "unconfirmed@test.com",
            EmailConfirmed = false,
        };
        _mockUserManager.Setup(m => m.FindByEmailAsync("unconfirmed@test.com")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.CheckPasswordAsync(user, "Password123!")).ReturnsAsync(true);

        // Even if PasswordSignInAsync would succeed, the handler should check EmailConfirmed
        _mockSignInManager
            .Setup(s => s.PasswordSignInAsync("unconfirmed@test.com", "Password123!", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _model.Input = new LoginModel.InputModel
        {
            Email = "unconfirmed@test.com",
            Password = "Password123!",
        };

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        Assert.Contains(
            _model.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage == "EmailNotConfirmed"
        );
    }

    [Fact]
    public async Task OnPostAsync_SuspendedUser_ReturnsPageWithSuspendedError()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            Email = "suspended@test.com",
            IsSuspended = true,
        };
        _mockUserManager.Setup(m => m.FindByEmailAsync("suspended@test.com")).ReturnsAsync(user);

        _model.Input = new LoginModel.InputModel
        {
            Email = "suspended@test.com",
            Password = "Password123!",
        };

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));
        _model.HttpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        // Act
        var result = await _model.OnPostAsync("/");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        Assert.Contains(
            _model.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage == "AccountSuspended"
        );
        _mockSignInManager.Verify(
            s =>
                s.PasswordSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()
                ),
            Times.Never
        );
    }
}
