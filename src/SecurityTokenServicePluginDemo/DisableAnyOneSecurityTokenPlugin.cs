using IdentityServer4.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenServicePluginDemo;

public static class DisableAnyOneSecurityTokenPlugin
{
    public static void RegisterServices(IServiceCollection services)
    {
        Console.WriteLine("register DisableAnyOneValidator...");
        services.AddTransient<IExtensionGrantValidator, DisableAnyOneValidator>();
    }
}
