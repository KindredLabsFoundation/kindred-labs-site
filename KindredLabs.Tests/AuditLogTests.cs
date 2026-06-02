using System.Security.Claims;
using KindredLabs.Core.Models.Identity;
using KindredLabs.Core.Services.Interfaces;
using KindredLabs.Web.Pages.Account.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

public class AuditLogTests
{
    private readonly Mock<ISecurityService> _mockSecurityService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<
        IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Security.AuditLog>
    > _mockLocalizer;
    private readonly AuditLogModel _model;

    public AuditLogTests()
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
            new Mock<IStringLocalizer<KindredLabs.Web.Resources.Pages.Account.Security.AuditLog>>();
        _mockLocalizer
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string name) => new LocalizedString(name, name));

        _model = new AuditLogModel(
            _mockSecurityService.Object,
            _mockUserManager.Object,
            _mockLocalizer.Object
        );

        var httpContext = new DefaultHttpContext();
        _model.PageContext = new PageContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task OnGetAsync_HappyPath_LoadsAndMapsEvents()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var logs = new List<SecurityAuditLog>
        {
            new SecurityAuditLog
            {
                PerformedAt = DateTime.UtcNow,
                EventType = SecurityEventTypes.PasswordChanged,
                Description = "Password updated",
                IpAddress = "::1",
            },
            new SecurityAuditLog
            {
                PerformedAt = DateTime.UtcNow.AddMinutes(-10),
                EventType = SecurityEventTypes.TwoFactorEnabled,
                Description = "2FA enabled",
                IpAddress = "1.2.3.4",
            },
        };

        _mockSecurityService.Setup(s => s.GetAuditLogAsync(user.Id, 100)).ReturnsAsync(logs);

        // Act
        var result = await _model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        var parsedLogs = _model.AuditLog.ToList();
        Assert.Equal(2, parsedLogs.Count);

        Assert.Equal("EventPasswordChanged", parsedLogs[0].DisplayEvent);
        Assert.Equal("localhost", parsedLogs[0].IpAddress);

        Assert.Equal("EventTwoFactorEnabled", parsedLogs[1].DisplayEvent);
        Assert.Equal("1.2.3.4", parsedLogs[1].IpAddress);
    }

    [Fact]
    public async Task OnGetAsync_UnknownEventType_DisplaysRawString()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var logs = new List<SecurityAuditLog>
        {
            new SecurityAuditLog { EventType = "UnknownEvent" },
        };

        _mockSecurityService.Setup(s => s.GetAuditLogAsync(user.Id, 100)).ReturnsAsync(logs);

        // Act
        await _model.OnGetAsync();

        // Assert
        var parsed = _model.AuditLog.First();
        Assert.Equal("UnknownEvent", parsed.DisplayEvent);
    }

    [Fact]
    public async Task OnGetAsync_Pagination_ReturnsCorrectSlice()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var logs = Enumerable
            .Range(1, 60)
            .Select(i => new SecurityAuditLog
            {
                PerformedAt = DateTime.UtcNow.AddMinutes(-i),
                EventType = SecurityEventTypes.EmailAdded,
            })
            .ToList();

        _mockSecurityService.Setup(s => s.GetAuditLogAsync(user.Id, 100)).ReturnsAsync(logs);

        // Act - Page 1
        await _model.OnGetAsync(1);
        Assert.Equal(25, _model.AuditLog.Count());
        Assert.Equal(3, _model.TotalPages);
        Assert.Equal(1, _model.CurrentPage);

        // Act - Page 2
        await _model.OnGetAsync(2);
        Assert.Equal(25, _model.AuditLog.Count());
        Assert.Equal(2, _model.CurrentPage);

        // Act - Page 3
        await _model.OnGetAsync(3);
        Assert.Equal(10, _model.AuditLog.Count());
    }

    [Fact]
    public async Task OnGetAsync_InvalidPageNumber_DefaultsToPage1()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockSecurityService
            .Setup(s => s.GetAuditLogAsync(user.Id, 100))
            .ReturnsAsync(new List<SecurityAuditLog> { new SecurityAuditLog() });

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
            .Setup(s => s.ExportAuditLogAsCsvAsync(user.Id))
            .ReturnsAsync("csv,content");

        // Act
        var result = await _model.OnGetDownloadAsync("csv");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains("kindredlabs_audit_log_", fileResult.FileDownloadName);
        Assert.EndsWith(".csv", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task OnGetDownloadAsync_TXT_ReturnsCorrectFile()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1" };
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _mockSecurityService
            .Setup(s => s.ExportAuditLogAsTxtAsync(user.Id))
            .ReturnsAsync("txt content");

        // Act
        var result = await _model.OnGetDownloadAsync("txt");

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Contains("kindredlabs_audit_log_", fileResult.FileDownloadName);
        Assert.EndsWith(".txt", fileResult.FileDownloadName);
    }
}
