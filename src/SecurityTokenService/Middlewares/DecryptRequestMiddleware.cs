using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SecurityTokenService.Utils;

namespace SecurityTokenService.Middlewares;

public class DecryptRequestMiddleware(RequestDelegate next)
{
    private const string VersionHeader = "Z-Encrypt-Version";
    private const string KeyHeader = "Z-Encrypt-Key";

    public async Task InvokeAsync(HttpContext context, ILogger<DecryptRequestMiddleware> logger)
    {
        var encryptVersion = context.Request.Headers[VersionHeader].ElementAtOrDefault(0);
        var encryptKey = context.Request.Headers[KeyHeader].ElementAtOrDefault(0);

        var encryptVersionIsNullOrEmpty = string.IsNullOrEmpty(encryptVersion);
        var encryptKeyIsNullOrEmpty = string.IsNullOrEmpty(encryptKey);

        // 若未传加密版本号和加密密钥， 则不解密
        if (encryptVersionIsNullOrEmpty && encryptKeyIsNullOrEmpty)
        {
            await next(context);
            return;
        }

        // 只有同时传了加密版本号和加密密钥，才解密
        if (!encryptKeyIsNullOrEmpty && !encryptVersionIsNullOrEmpty)
        {
            try
            {
                switch (encryptVersion)
                {
                    case "v1.0":
                    {
                        // KEY 不做转换
                        break;
                    }
                    case "v1.1":
                    {
                        encryptKey = GetRealKeyV11(encryptKey);
                        break;
                    }
                    default:
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                }

                using var ase = Util.CreateAesEcb(encryptKey);
                // 前端固定对称加密的 KEY，仅应用对 WF 对一些敏感数据的拦截。
                await DecryptV1Body(context, ase, logger);
            }
            catch (Exception e)
            {
                logger.LogError(e, "解密请求体出错");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                // 解密失败则不往下执行了
                return;
            }

            await next(context);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }

        // 若未传加密密钥，则不解密
    }

    private static string GetRealKeyV11(string encryptKey)
    {
        var p1 = encryptKey.Substring(0, 10);
        var p2 = encryptKey.Substring(16, encryptKey.Length - 16);
        encryptKey = p1 + p2;
        return encryptKey;
    }

    private static async Task DecryptV1Body(HttpContext context, Aes aes, ILogger<DecryptRequestMiddleware> logger)
    {
        using var streamReader = new StreamReader(context.Request.Body);
        var bodyContent = await streamReader.ReadToEndAsync();
        if (!string.IsNullOrEmpty(bodyContent))
        {
            // 处理兼容问题,替换掉双引号的字符串
            var replaceBodyContent = bodyContent.Replace("\"", "");
            // 解密请求body
            var decryptedBody = Util.AesEcbDecrypt(aes, replaceBodyContent);
            logger.LogDebug($"DecryptRequestMiddleware_V1_解密:{System.Text.Encoding.UTF8.GetString(decryptedBody)}");
            context.Request.Body = new MemoryStream(decryptedBody);
        }
    }
}
