using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace KindredLabs.Core.Services.Implementations;

/// <summary>
/// Implementation of <see cref="IEncryptionService"/> using ASP.NET Core Data Protection.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionService"/> class.
    /// </summary>
    /// <param name="provider">The data protection provider.</param>
    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("FormData");
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;
        return _protector.Protect(plaintext);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return ciphertext;
        return _protector.Unprotect(ciphertext);
    }
}
