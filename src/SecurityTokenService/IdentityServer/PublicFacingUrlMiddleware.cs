using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SecurityTokenService.IdentityServer;

public class PublicFacingUrlMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task Invoke(HttpContext context)
    {
        var schema = configuration["IdentityServer:Scheme"];
        if (schema == "http")
        {
            context.Request.Scheme = "http";
            context.Request.IsHttps = false;
        }
        else if (schema == "https")
        {
            context.Request.Scheme = "https";
            context.Request.IsHttps = true;
        }

        var basePath = configuration["IdentityServer:BasePath"];
        if (!string.IsNullOrEmpty(basePath))
        {
            context.SetIdentityServerBasePath(basePath);
        }

        var origin = configuration["IdentityServer:Origin"];
        if (!string.IsNullOrEmpty(origin))
        {
            context.SetIdentityServerOrigin(origin);
        }

        await next(context);
    }
}
