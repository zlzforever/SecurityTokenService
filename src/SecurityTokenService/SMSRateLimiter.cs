using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SecurityTokenService.Controllers;
using SecurityTokenService.Extensions;

namespace SecurityTokenService;

public class SMSRateLimiter(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (!"/account/sms".Equals(path, StringComparison.OrdinalIgnoreCase) &&
            !"/account/sendsmscode".Equals(path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string ip = context.GetRemoteIpAddressString();
        if (string.IsNullOrEmpty(ip))
        {
            return;
        }

        // 获取当前时间

        var cache = context.RequestServices.GetRequiredService<IMemoryCache>();

        var key = $"SMS:IP:LIMIT:{ip}";
        var timestamp = cache.Get<long?>(key);
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (timestamp != null && now - timestamp < 60)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsJsonAsync(new ApiResult
            {
                Code = 429, Message = "验证码发送过于频繁，请稍后再试", Success = false
            });
            return;
        }

        cache.Set(key, now, TimeSpan.FromSeconds(60));
        await next(context);
    }
}
