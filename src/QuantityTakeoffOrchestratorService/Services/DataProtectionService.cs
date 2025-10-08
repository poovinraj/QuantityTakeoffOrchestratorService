using Azure.Security.KeyVault.Keys.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace QuantityTakeoffOrchestratorService.Services;

/// <summary>
///     Data protection service using encryption and decryption methods
/// </summary>
[ExcludeFromCodeCoverage]
public class DataProtectionService : IDataProtectionService
{
    private readonly CryptographyClient _cryptographyClient;

    /// <summary>
    ///     Data protection service constructor
    /// </summary>
    /// <param name="cryptographyClient">Azure Key Vault cryptography client</param>
    /// <exception cref="ArgumentNullException">Thrown when cryptographyClient is null</exception>
    public DataProtectionService(CryptographyClient cryptographyClient)
    {
        _cryptographyClient = cryptographyClient ?? throw new ArgumentNullException(nameof(cryptographyClient));
    }

    /// <summary>
    ///     Encrypt a plain text
    /// </summary>
    /// <param name="plaintext">Data to encrypt</param>
    /// <returns>Encrypted data as byte array</returns>
    /// <exception cref="ArgumentNullException">Thrown when plaintext is null</exception>
    public async Task<byte[]> Encrypt(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var encryptResult = await _cryptographyClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plaintext);

        return encryptResult.Ciphertext;
    }

    /// <summary>
    ///     Decrypt an encrypted text
    /// </summary>
    /// <param name="encryptedText">Data to decrypt</param>
    /// <returns>Decrypted data as byte array</returns>
    /// <exception cref="ArgumentNullException">Thrown when encryptedText is null</exception>
    public async Task<byte[]> Decrypt(byte[] encryptedText)
    {
        ArgumentNullException.ThrowIfNull(encryptedText);

        var decryptResult = await _cryptographyClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, encryptedText);

        return decryptResult.Plaintext;
    }
}
