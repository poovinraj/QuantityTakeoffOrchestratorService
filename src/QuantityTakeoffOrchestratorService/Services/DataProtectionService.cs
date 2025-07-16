using Azure.Security.KeyVault.Keys.Cryptography;

namespace QuantityTakeoffOrchestratorService.Services;

/// <summary>
///     Data protection service using encryption and decryption methods
/// </summary>
public class DataProtectionService : IDataProtectionService
{
    private readonly CryptographyClient _cryptographyClient;

    /// <summary>
    ///     Data protection service constructor
    /// </summary>
    /// <param name="cryptographyClient"></param>
    public DataProtectionService(CryptographyClient cryptographyClient)
    {
        _cryptographyClient = cryptographyClient;
    }

    /// <summary>
    ///     Encrypt a plain text
    /// </summary>
    /// <param name="plaintext"></param>
    public async Task<byte[]> Encrypt(byte[] plaintext)
    {
        var encryptResult = await _cryptographyClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plaintext);

        return encryptResult.Ciphertext;
    }

    /// <summary>
    ///     Decrypt an encrypted text
    /// </summary>
    /// <param name="encryptedText"></param>
    public async Task<byte[]> Decrypt(byte[] encryptedText)
    {
        var decryptResult = await _cryptographyClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, encryptedText);

        return decryptResult.Plaintext;
    }
}
