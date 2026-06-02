using System.Security.Claims;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class RegisterTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IUserStore<ApplicationUser>> _mockUserStore;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<ILogger<RegisterModel>> _mockLogger;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Register>
    > _mockLocalizer;
    private readonly RegisterModel _model;

    public RegisterTests()
    {
        // Must implement IUserEmailStore for the model constructor
        _mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserStore.As<IUserEmailStore<ApplicationUser>>();

        var mockCultureFeature = new Mock<IRequestCultureFeature>();
        mockCultureFeature.Setup(f => f.RequestCulture).Returns(new RequestCulture("en"));

        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IRequestCultureFeature>(mockCultureFeature.Object);

        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            _mockUserStore.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!
        );
        _mockUserManager.Setup(m => m.SupportsUserEmail).Returns(true);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
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

        _mockLogger = new Mock<ILogger<RegisterModel>>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLocalizer =
            new Mock<IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Register>>();

        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        _model = new RegisterModel(
            _mockUserManager.Object,
            _mockUserStore.As<IUserEmailStore<ApplicationUser>>().Object, // Passed explicitly as IUserEmailStore
            _mockSignInManager.Object,
            _mockLogger.Object,
            _mockEmailService.Object,
            _mockLocalizer.Object
        );

        _model.PageContext = new PageContext { HttpContext = httpContext };
        _model.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("http://localhost/callback")
            .Verifiable();
        mockUrlHelper
            .SetupGet(x => x.ActionContext)
            .Returns(
                new ActionContext
                {
                    HttpContext = httpContext,
                    RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                    ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
                }
            );
        mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(true);
        mockUrlHelper
            .Setup(x => x.Content(It.IsAny<string>()))
            .Returns((string content) => content);
        _model.Url = mockUrlHelper.Object;
    }

    [Fact]
    public async Task OnPostAsync_SuccessfulRegistration_PersistsAllFields()
    {
        // Arrange
        _model.Input = new RegisterModel.InputModel
        {
            Email = "newuser@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Alice",
            LastName = "Wonder",
            Organization = "Wonderland",
            JobTitle = "Explorer",
        };

        var emailStoreMock = _mockUserStore.As<IUserEmailStore<ApplicationUser>>();
        emailStoreMock
            .Setup(s =>
                s.SetUserNameAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);
        emailStoreMock
            .Setup(s =>
                s.SetEmailAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);
        emailStoreMock
            .Setup(s =>
                s.GetNormalizedEmailAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync("NEWUSER@TEST.COM");

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("token");

        // Act
        var result = await _model.OnPostAsync();

        // Assert
        _mockUserManager.Verify(
            m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password123!"),
            Times.Once
        );
        _mockUserManager.Verify(
            m =>
                m.UpdateAsync(
                    It.Is<ApplicationUser>(u =>
                        u.FirstName == "Alice"
                        && u.LastName == "Wonder"
                        && u.Organization == "Wonderland"
                        && u.JobTitle == "Explorer"
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task OnPostAsync_DuplicateEmail_ReturnsPageWithErrors()
    {
        // Arrange
        _model.Input = new RegisterModel.InputModel
        {
            Email = "duplicate@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Alice",
            LastName = "Wonder",
        };

        var error = new IdentityError
        {
            Code = "DuplicateEmail",
            Description = "Email already exists",
        };
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        Assert.True(
            _model.ModelState.Values.Any(v =>
                v.Errors.Any(e => e.ErrorMessage == "Email already exists")
            )
        );
        _mockEmailService.Verify(
            e => e.SendRegistrationConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task OnPostAsync_WeakPassword_ReturnsPageWithErrors()
    {
        // Arrange
        _model.Input = new RegisterModel.InputModel
        {
            Email = "weak@test.com",
            Password = "123",
            ConfirmPassword = "123",
            FirstName = "Alice",
            LastName = "Wonder",
        };

        var error = new IdentityError
        {
            Code = "PasswordTooShort",
            Description = "Password must be at least 6 characters",
        };
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        Assert.True(
            _model.ModelState.Values.Any(v =>
                v.Errors.Any(e => e.ErrorMessage == "Password must be at least 6 characters")
            )
        );
    }

    [Fact]
    public async Task OnPostAsync_UpdateAsyncFailure_RollsBackUser()
    {
        // Arrange
        _model.Input = new RegisterModel.InputModel
        {
            Email = "rollback@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Alice",
            LastName = "Wonder",
        };

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var error = new IdentityError { Code = "UpdateFailed", Description = "Update failed" };
        _mockUserManager
            .Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(error));

        _mockUserManager
            .Setup(m => m.DeleteAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Verifiable();

        // Act
        var result = await _model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(_model.ModelState.IsValid);
        Assert.True(
            _model.ModelState.Values.Any(v => v.Errors.Any(e => e.ErrorMessage == "Update failed"))
        );
        _mockUserManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_MissingFirstName_ReturnsPageWithoutCallingCreateAsync()
    {
        // Arrange
        _model.Input = new RegisterModel.InputModel
        {
            Email = "missing@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "", // Missing
            LastName = "Wonder",
        };
        _model.ModelState.AddModelError("Input.FirstName", "The First name field is required.");

        // Act
        var result = await _model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        _mockUserManager.Verify(
            m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task OnPostAsync_MissingLastName_ReturnsPageWithoutCallingCreateAsync()
    {
        // Arrange
        _model.Input = new RegisterModel.InputModel
        {
            Email = "missing@test.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            FirstName = "Alice",
            LastName = "", // Missing
        };
        _model.ModelState.AddModelError("Input.LastName", "The Last name field is required.");

        // Act
        var result = await _model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        _mockUserManager.Verify(
            m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never
        );
    }
}
