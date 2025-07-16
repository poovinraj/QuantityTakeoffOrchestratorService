using System.Security.Cryptography;
using System.Text;

namespace QuantityTakeoffOrchestratorService.Services;


/// <summary>
///     Encryption service using AES algorithm
/// </summary>
public class AesEncryptionService : IAesEncryptionService
{
    /// <summary>
    ///     Encrypt a plain text using AES algorithm
    /// </summary>
    /// <param name="plainText"></param>
    /// <param name="key"></param>
    public byte[] Encrypt(string plainText, byte[] key)
    {
        var buffer = Encoding.UTF8.GetBytes(plainText);

        using var inputStream = new MemoryStream(buffer, false);
        using var outputStream = new MemoryStream();
        using var aes = Aes.Create();
        aes.Key = key;
        var iv = aes.IV;
        outputStream.Write(iv, 0, iv.Length);
        outputStream.Flush();

        var encryptFunction = aes.CreateEncryptor(key, iv);
        using var cryptoStream = new CryptoStream(outputStream, encryptFunction, CryptoStreamMode.Write);
        inputStream.CopyTo(cryptoStream);
        cryptoStream.FlushFinalBlock();

        return outputStream.ToArray();
    }

    /// <summary>
    ///     Decrypt an encrypted text using AES algorithm
    /// </summary>
    /// <param name="encryptedText"></param>
    /// <param name="key"></param>
    public string Decrypt(byte[] encryptedText, byte[] key)
    {
        using var inputStream = new MemoryStream(encryptedText, false);
        using var outputStream = new MemoryStream();
        using var aes = Aes.Create();
        aes.Key = key;
        var iv = new byte[16];
        var bytesRead = inputStream.Read(iv, 0, 16);
        if (bytesRead < 16)
        {
            throw new CryptographicException("IV is missing or invalid.");
        }

        var decryptFunction = aes.CreateDecryptor(key, iv);
        using var cryptoStream = new CryptoStream(inputStream, decryptFunction, CryptoStreamMode.Read);
        cryptoStream.CopyTo(outputStream);

        var decryptedValue = Encoding.UTF8.GetString(outputStream.ToArray());
        return decryptedValue;
    }
}
