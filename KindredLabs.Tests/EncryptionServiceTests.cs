using KindredLabs.Core.Services.Implementations;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using Xunit;

namespace KindredLabs.Tests;

/// <summary>
/// Tests for the <see cref="EncryptionService"/>.
/// </summary>
public class EncryptionServiceTests
{
    private readonly Mock<IDataProtectionProvider> _mockProvider;
    private readonly Mock<IDataProtector> _mockProtector;
    private readonly EncryptionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionServiceTests"/> class.
    /// </summary>
    public EncryptionServiceTests()
    {
        _mockProvider = new Mock<IDataProtectionProvider>();
        _mockProtector = new Mock<IDataProtector>();

        _mockProvider
            .Setup(p => p.CreateProtector(It.IsAny<string>()))
            .Returns(_mockProtector.Object);

        _service = new EncryptionService(_mockProvider.Object);
    }

    /// <summary>
    /// Verifies that Encrypt returns a different string than the input.
    /// </summary>
    [Fact]
    public void Encrypt_ReturnsDifferentString()
    {
        // Arrange
        var input = "plain text";
        var encryptedBytes = System.Text.Encoding.UTF8.GetBytes("encrypted text");
        _mockProtector.Setup(p => p.Protect(It.IsAny<byte[]>())).Returns(encryptedBytes);

        // Act
        var result = _service.Encrypt(input);

        // Assert
        Assert.NotEqual(input, result);
        // The DataProtection extension method for strings typically returns a Base64-encoded string of the protected bytes
        Assert.Equal(
            Convert.ToBase64String(encryptedBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
            result
        );
    }

    /// <summary>
    /// Verifies that Decrypt(Encrypt(input)) returns the original input.
    /// </summary>
    [Fact]
    public void Decrypt_Encrypt_ReturnsOriginalInput()
    {
        // Arrange
        var input = "plain text";
        var encrypted = "encrypted text";
        _mockProtector
            .Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns(System.Text.Encoding.UTF8.GetBytes(encrypted));
        _mockProtector
            .Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Returns(System.Text.Encoding.UTF8.GetBytes(input));

        // Act
        var encryptedResult = _service.Encrypt(input);
        var decryptedResult = _service.Decrypt(encryptedResult);

        // Assert
        Assert.Equal(input, decryptedResult);
    }

    /// <summary>
    /// Verifies that empty string is returned as-is without encrypting.
    /// </summary>
    [Fact]
    public void Encrypt_EmptyString_ReturnsAsIs()
    {
        // Arrange
        var input = "";

        // Act
        var result = _service.Encrypt(input);

        // Assert
        Assert.Equal(input, result);
        _mockProtector.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Never);
    }

    /// <summary>
    /// Verifies that null string is returned as-is without encrypting.
    /// </summary>
    [Fact]
    public void Encrypt_NullString_ReturnsAsIs()
    {
        // Arrange
        string? input = null;

        // Act
        var result = _service.Encrypt(input!);

        // Assert
        Assert.Null(result);
        _mockProtector.Verify(p => p.Protect(It.IsAny<byte[]>()), Times.Never);
    }
}
