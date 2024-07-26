using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SecurityTokenService.IdentityServer;

public class PublicFacingUrlMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task Invoke(HttpContext context)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<PublicFacingUrlMiddleware>>();
        var schema = configuration["IdentityServer:Scheme"];
        logger.LogInformation("Invoking PublicFacingUrlMiddleware");
        if (schema == "http")
        {
            context.Request.Scheme = "http";
            context.Request.IsHttps = false;
            logger.LogInformation("Setting scheme to http");
        }
        else if (schema == "https")
        {
            context.Request.Scheme = "https";
            context.Request.IsHttps = true;
            logger.LogInformation("Setting scheme to https");
        }

        var basePath = configuration["IdentityServer:BasePath"];
        if (!string.IsNullOrEmpty(basePath))
        {
            logger.LogInformation("Setting base path to {BasePath}", basePath);
            context.SetIdentityServerBasePath(basePath);
        }

        var origin = configuration["IdentityServer:Origin"];
        if (!string.IsNullOrEmpty(origin))
        {
            logger.LogInformation("Setting base path to {Origin}", origin);
            context.SetIdentityServerOrigin(origin);
        }

        await next(context);
    }
}
