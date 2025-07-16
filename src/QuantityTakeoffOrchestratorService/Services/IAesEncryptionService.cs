namespace QuantityTakeoffOrchestratorService.Services;

/// <summary>
///     Encryption service using AES algorithm
/// </summary>
public interface IAesEncryptionService
{
    /// <summary>
    ///     Encrypt a plain text using AES algorithm
    /// </summary>
    /// <param name="plainText"></param>
    /// <param name="key"></param>
    byte[] Encrypt(string plainText, byte[] key);

    /// <summary>
    ///     Decrypt an encrypted text using AES algorithm
    /// </summary>
    /// <param name="encryptedText"></param>
    /// <param name="key"></param>
    string Decrypt(byte[] encryptedText, byte[] key);
}
