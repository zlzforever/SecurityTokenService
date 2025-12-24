using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SecurityTokenService;

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
    public const string PurposeResetPassword = "ResetPassword";
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
}
