using System.Security.Cryptography;
using System.Text;

namespace TimeOfficeSync.Services;

/// <summary>
/// Central AES helper. Key always comes from user input:
/// <c>EncryptionSettings:Key</c> in appsettings.json (TimeOfficeSync)
/// or interactive input / --key flag (LicenseGen).
/// No hardcoded default.
/// </summary>
public static class CryptoHelper
{
    public static string GetPassphrase(IConfiguration configuration)
    {
        var key = configuration["EncryptionSettings:Key"]?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "EncryptionSettings:Key is missing. Please enter/provide Encryption Key as user input in appsettings.json.");
        }
        return key;
    }

    public static byte[] GetKeyBytes(string passphrase)
    {
        var bytes = Encoding.UTF8.GetBytes(passphrase);
        // Valid AES lengths used as-is (preserves compat with old data),
        // any other length derived to 256-bit via SHA256 so any input value works.
        if (bytes.Length == 16 || bytes.Length == 24 || bytes.Length == 32)
            return bytes;
        return SHA256.HashData(bytes);
    }

    public static string Encrypt(string plainText, string passphrase)
    {
        using var aes = Aes.Create();
        aes.Key = GetKeyBytes(passphrase);
        aes.IV = new byte[16];

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs)) { sw.Write(plainText); }
        return Convert.ToBase64String(ms.ToArray());
    }

    public static string DecryptToString(string cipherText, string passphrase)
    {
        using var aes = Aes.Create();
        aes.Key = GetKeyBytes(passphrase);
        aes.IV = new byte[16];

        var buffer = Convert.FromBase64String(cipherText);
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
