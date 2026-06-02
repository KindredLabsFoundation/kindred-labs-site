using System.Security.Claims;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Pages.Account.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class LoginHistoryTests
{
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Security.LoginHistory>
    > _mockLocalizer;
    private readonly LoginHistoryModel _model;

    public LoginHistoryTests()
    {
        _mockSecurityService = new Mock<ISecurityService>();

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

        _mockLocalizer =
            new Mock<
                IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Security.LoginHistory>
            >();
        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        _model = new LoginHistoryModel(
            _mockSecurityService.Object,
            _mockUserManager.Object,
            _mockLocalizer.Object
        );

        var httpContext = new DefaultHttpContext();
        _model.PageContext = new PageContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task OnGetAsync_HappyPath_LoadsAndParsesHistory()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var history = new List<LoginHistory>
        {
            new LoginHistory
            {
                LoginAt = DateTime.UtcNow,
                IpAddress = "::1",
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                Success = true,
            },
            new LoginHistory
            {
                LoginAt = DateTime.UtcNow.AddMinutes(-10),
                IpAddress = "192.168.1.1",
                UserAgent =
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Firefox/120.0",
                Success = false,
            },
        };

        _mockSecurityService.Setup(s => s.GetLoginHistoryAsync(user.Id, 100)).ReturnsAsync(history);

        // Act
        var result = await _model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        var parsedHistory = _model.LoginHistory.ToList();
        Assert.Equal(2, parsedHistory.Count);

        Assert.Equal("localhost", parsedHistory[0].IpAddress);
        Assert.Equal("Chrome on Windows", parsedHistory[0].ParsedUserAgent);

        Assert.Equal("192.168.1.1", parsedHistory[1].IpAddress);
        Assert.Equal("Firefox on macOS", parsedHistory[1].ParsedUserAgent);
    }

    [Fact]
    public async Task OnGetAsync_Pagination_ReturnsCorrectSlice()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var history = Enumerable
            .Range(1, 60)
            .Select(i => new LoginHistory
            {
                LoginAt = DateTime.UtcNow.AddMinutes(-i),
                IpAddress = "1.1.1.1",
                UserAgent = "Chrome",
            })
            .ToList();

        _mockSecurityService.Setup(s => s.GetLoginHistoryAsync(user.Id, 100)).ReturnsAsync(history);

        // Act - Page 1
        await _model.OnGetAsync(1);
        var page1 = _model.LoginHistory.ToList();
        Assert.Equal(25, page1.Count);
        Assert.Equal(3, _model.TotalPages);
        Assert.Equal(1, _model.CurrentPage);

        // Act - Page 2
        await _model.OnGetAsync(2);
        var page2 = _model.LoginHistory.ToList();
        Assert.Equal(25, page2.Count);
        Assert.Equal(2, _model.CurrentPage);

        // Act - Page 3
        await _model.OnGetAsync(3);
        var page3 = _model.LoginHistory.ToList();
        Assert.Equal(10, page3.Count);
    }

    [Fact]
    public async Task OnGetAsync_UnknownUserAgent_FallsBackToSubstring()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        string rawUa = "Some Weird Browser/1.0 (No Known OS) " + new string('a', 60);
        var history = new List<LoginHistory>
        {
            new LoginHistory { LoginAt = DateTime.UtcNow, UserAgent = rawUa },
        };

        _mockSecurityService.Setup(s => s.GetLoginHistoryAsync(user.Id, 100)).ReturnsAsync(history);

        // Act
        await _model.OnGetAsync();

        // Assert
        var parsed = _model.LoginHistory.First();
        Assert.Equal(rawUa.Substring(0, 50), parsed.ParsedUserAgent);
    }

    [Fact]
    public async Task OnGetAsync_InvalidPageNumber_DefaultsToPage1()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockSecurityService
            .Setup(s => s.GetLoginHistoryAsync(user.Id, 100))
            .ReturnsAsync(new List<LoginHistory> { new LoginHistory() });

        // Act
        await _model.OnGetAsync(0);
        Assert.Equal(1, _model.CurrentPage);

        await _model.OnGetAsync(-5);
        Assert.Equal(1, _model.CurrentPage);
    }

    [Fact]
    public async Task OnGetDownloadAsync_CSV_ReturnsCorrectFile()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockSecurityService
            .Setup(s => s.ExportLoginHistoryAsCsvAsync(user.Id))
            .ReturnsAsync("csv,content");

        // Act
        var result = await _model.OnGetDownloadAsync("csv");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("kindredlabs_login_history_", fileResult.FileDownloadName);
        Assert.EndsWith(".csv", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task OnGetDownloadAsync_TXT_ReturnsCorrectFile()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockSecurityService
            .Setup(s => s.ExportLoginHistoryAsTxtAsync(user.Id))
            .ReturnsAsync("txt content");

        // Act
        var result = await _model.OnGetDownloadAsync("txt");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Contains("kindredlabs_login_history_", fileResult.FileDownloadName);
        Assert.EndsWith(".txt", fileResult.FileDownloadName);
    }
}
