using KindredLabs.Core.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace KindredLabs.Core.Services.Implementations;

public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("FormData");
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;
        return _protector.Protect(plaintext);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return ciphertext;
        return _protector.Unprotect(ciphertext);
    }
}
