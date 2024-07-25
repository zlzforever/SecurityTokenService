using System;
using System.Security.Cryptography;
using System.Text;

namespace SecurityTokenService;

public static class Util
{
    public static readonly Aes DataProtectionKeyAes = Aes.Create();

    public static string Encrypt(Aes aes, string v)
    {
        return Convert.ToBase64String(aes.EncryptEcb(Encoding.UTF8.GetBytes(v), PaddingMode.PKCS7));
    }

    public static string Decrypt(Aes aes, string v)
    {
        return Encoding.UTF8.GetString(aes.DecryptEcb(Convert.FromBase64String(v), PaddingMode.PKCS7));
    }
}
