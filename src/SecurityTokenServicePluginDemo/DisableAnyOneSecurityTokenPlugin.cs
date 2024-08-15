using IdentityServer4.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SecurityTokenServicePluginDemo;

public static class DisableAnyOneSecurityTokenPlugin
{
    public static void Load(IHostApplicationBuilder builder)
    {
        Console.WriteLine("Load DisableAnyOneSecurityTokenPlugin");
        builder.Services.AddTransient<IExtensionGrantValidator, DisableAnyOneValidator>();
    }
}
