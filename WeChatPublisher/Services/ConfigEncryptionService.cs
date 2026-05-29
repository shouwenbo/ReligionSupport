using System.Security.Cryptography;
using System.Text;

namespace WeChatPublisher.Services;

public static class ConfigEncryptionService
{
    private static readonly byte[] Entropy = "WeChatPublisher.Salt.2026"u8.ToArray();

    public static string Encrypt(string plainText)
    {
        var data = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string cipherBase64)
    {
        var data = Convert.FromBase64String(cipherBase64);
        var decrypted = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }

    public static bool IsEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        try { Convert.FromBase64String(value); return true; }
        catch { return false; }
    }
}
