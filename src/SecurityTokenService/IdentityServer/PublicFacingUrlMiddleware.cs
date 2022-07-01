using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SecurityTokenService.IdentityServer;

public class PublicFacingUrlMiddleware
{
    private readonly RequestDelegate _next;

    public PublicFacingUrlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Request.Scheme = "https";
        context.Request.IsHttps = true;

        await _next(context);
    }
}