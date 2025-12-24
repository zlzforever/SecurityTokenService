using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityTokenService.Utils;

namespace SecurityTokenService.Controllers;

[SecurityHeaders]
[AllowAnonymous]
[Route("api/v1.0/captcha")]
public class CaptchaController(
    IMemoryCache memoryCache,
    ILogger<CaptchaController> logger,
    IOptionsMonitor<SecurityTokenServiceOptions> securityTokenServiceOptions) : ControllerBase
{
    /// <summary>
    /// TODO: 若有多个实例，需要使用分布式缓存
    /// </summary>
    /// <returns></returns>
    [HttpGet("generate")]
    public IActionResult Generate()
    {
        string captchaId = Guid.NewGuid().ToString("N");
        var code = VerifyCodeHelper.GenerateCode(securityTokenServiceOptions.CurrentValue.GetVerifyCodeLength());
        var cacheKey = string.Format(Util.CaptchaTtlKey, captchaId);
        Response.Cookies.Append(Util.CaptchaId, captchaId);
        var bytes = VerifyCodeHelper.GetVerifyCode(code);
        memoryCache.Set(cacheKey, code, TimeSpan.FromMinutes(2));
        logger.LogDebug("[CAPTCHA] {CaptchaId} {CaptchaCode}", captchaId, code);
        return File(bytes, "image/png");
    }
}
