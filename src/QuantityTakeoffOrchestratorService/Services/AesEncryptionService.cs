using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace QuantityTakeoffOrchestratorService.Services;

/// <summary>
///     Encryption service using AES algorithm
/// </summary>
[ExcludeFromCodeCoverage]
public class AesEncryptionService : IAesEncryptionService
{
    private const int IvSize = 16; // AES IV size is always 16 bytes (128 bits)

    /// <summary>
    ///     Encrypt a plain text using AES algorithm
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <param name="key">The encryption key</param>
    /// <returns>Encrypted data as byte array with IV prepended</returns>
    /// <exception cref="ArgumentNullException">Thrown when plainText or key is null</exception>
    /// <exception cref="CryptographicException">Thrown when key size is invalid</exception>
    public byte[] Encrypt(string plainText, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        ArgumentNullException.ThrowIfNull(key);

        // Convert plain text to bytes
        var buffer = Encoding.UTF8.GetBytes(plainText);

        using var outputStream = new MemoryStream();
        using var aes = Aes.Create();
        
        // Set the key and generate a new IV
        aes.Key = key;
        var iv = aes.IV;
        
        // Write IV to the beginning of the output stream
        outputStream.Write(iv, 0, iv.Length);

        // Create encryptor and crypto stream
        using (var encryptFunction = aes.CreateEncryptor(key, iv))
        using (var cryptoStream = new CryptoStream(outputStream, encryptFunction, CryptoStreamMode.Write))
        using (var inputStream = new MemoryStream(buffer))
        {
            inputStream.CopyTo(cryptoStream);
            cryptoStream.FlushFinalBlock();
        }

        return outputStream.ToArray();
    }

    /// <summary>
    ///     Decrypt an encrypted text using AES algorithm
    /// </summary>
    /// <param name="encryptedText">The encrypted data with IV prepended</param>
    /// <param name="key">The decryption key</param>
    /// <returns>Decrypted text</returns>
    /// <exception cref="ArgumentNullException">Thrown when encryptedText or key is null</exception>
    /// <exception cref="CryptographicException">Thrown when IV is missing/invalid or decryption fails</exception>
    public string Decrypt(byte[] encryptedText, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(encryptedText);
        ArgumentNullException.ThrowIfNull(key);

        if (encryptedText.Length < IvSize)
        {
            throw new CryptographicException("Encrypted data is too short to contain a valid IV.");
        }

        using var inputStream = new MemoryStream(encryptedText);
        using var outputStream = new MemoryStream();
        using var aes = Aes.Create();
        
        // Set the key
        aes.Key = key;
        
        // Read the IV from the beginning of the encrypted data
        var iv = new byte[IvSize];
        var bytesRead = inputStream.Read(iv, 0, IvSize);
        if (bytesRead < IvSize)
        {
            throw new CryptographicException("IV is missing or invalid.");
        }

        // Create decryptor and crypto stream
        using (var decryptFunction = aes.CreateDecryptor(key, iv))
        using (var cryptoStream = new CryptoStream(inputStream, decryptFunction, CryptoStreamMode.Read))
        {
            cryptoStream.CopyTo(outputStream);
        }

        // Convert decrypted bytes to string
        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
