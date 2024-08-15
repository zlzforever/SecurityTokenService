using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurityTokenServicePluginDemo.Controllers;

namespace SecurityTokenServicePluginDemo;

public static class DisableAnyOneSecurityTokenPlugin
{
    public static void Load(IHostApplicationBuilder builder)
    {
        Console.WriteLine("Load DisableAnyOneSecurityTokenPlugin");
        builder.Services.AddTransient<IExtensionGrantValidator, DisableAnyOneValidator>();
        builder.Services.AddControllers().AddApplicationPart(typeof(TestController).Assembly);
    }

    public static void Use(WebApplication app)
    {
        Console.WriteLine("Use DisableAnyOneSecurityTokenPlugin");
    }
}
