namespace QuantityTakeoffOrchestratorService.Services;

/// <summary>
///     IDataProtectionService  
/// </summary>
public interface IDataProtectionService
{
    /// <summary>
    ///     Encrypt a plain text
    /// </summary>
    /// <param name="plaintext"></param>
    Task<byte[]> Encrypt(byte[] plaintext);

    /// <summary>
    ///     Decrypt an encrypted text
    /// </summary>
    /// <param name="encryptedText"></param>
    Task<byte[]> Decrypt(byte[] encryptedText);
}