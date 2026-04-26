namespace KindredLabs.Core.Services.Interfaces;

/// <summary>
/// Provides methods for encrypting and decrypting sensitive strings.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plaintext.
    /// </summary>
    /// <param name="plaintext">The text to encrypt.</param>
    /// <returns>The encrypted ciphertext.</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts the specified ciphertext.
    /// </summary>
    /// <param name="ciphertext">The encrypted text to decrypt.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when decryption fails.</exception>
    string Decrypt(string ciphertext);
}
