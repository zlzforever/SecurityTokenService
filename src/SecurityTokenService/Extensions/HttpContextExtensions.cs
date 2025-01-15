using System.Linq;
using Microsoft.AspNetCore.Http;

namespace SecurityTokenService.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetRemoteIpAddressString(this HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(forwardedFor))
        {
            forwardedFor = context.Connection.RemoteIpAddress?.ToString();
        }

        return forwardedFor;
    }
}
