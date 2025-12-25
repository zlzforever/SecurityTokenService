using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SecurityTokenService.Utils;

public static class Util
{
    // public static Aes DataProtectionKeyAes;
    //
    // public static string Encrypt(Aes aes, string v)
    // {
    //     return Convert.ToBase64String(aes.EncryptEcb(Encoding.UTF8.GetBytes(v), PaddingMode.PKCS7));
    // }
    //
    // public static string Decrypt(Aes aes, string v)
    // {
    //     return Encoding.UTF8.GetString(aes.DecryptEcb(Convert.FromBase64String(v), PaddingMode.PKCS7));
    // }

    public const string CaptchaId = "CaptchaId";
    public const string CaptchaTtlKey = "CaptchaId:{0}";
    public const string PhoneNumberTokenProvider = "PhoneNumberTokenProvider";
    public const string PurposeLogin = "Login";
    public const string PurposeRegister = "Register";

    public static void GenerateCertificate(string path)
    {
        // 设置证书的有效期
        var startDate = DateTime.Now;
        var endDate = DateTime.Now.AddYears(100);

        // 创建一个新的自签名证书
        using var rsa = new RSACryptoServiceProvider(2048);
        var request = new CertificateRequest(
            new X500DistinguishedName("CN=Self-Signed Certificate"),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // 设置证书的扩展
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        var certificate = request.CreateSelfSigned(startDate, endDate);

        // 将证书保存到文件
        var certBytes = certificate.Export(X509ContentType.Pfx);
        System.IO.File.WriteAllBytes(path, certBytes);
    }

    public static Aes CreateAesEcb(string key)
    {
        var keyArray = Encoding.UTF8.GetBytes(key);
        var aes = Aes.Create();
        aes.Key = keyArray;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        return aes;
    }

    public static string CreateAesKey()
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 128; // 可以设置为 128、192 或 256 位
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }

    public static byte[] AesEcbDecrypt(Aes aes, string text)
    {
        var toEncryptArray = Convert.FromBase64String(text);
        using var decrypt = aes.CreateDecryptor();
        return decrypt.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
    }
}
