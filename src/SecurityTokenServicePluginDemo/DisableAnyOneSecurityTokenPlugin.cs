using IdentityServer4.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenServicePluginDemo;

public static class DisableAnyOneSecurityTokenPlugin
{
    public static void RegisterServices(IServiceCollection services)
    {
        Console.WriteLine("Load DisableAnyOneSecurityTokenPlugin");
        services.AddTransient<IExtensionGrantValidator, DisableAnyOneValidator>();
    }
}
