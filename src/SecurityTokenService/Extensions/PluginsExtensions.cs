using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SecurityTokenService.Extensions;

public static class PluginsExtensions
{
    public static void LoadPlugins(this IServiceCollection services)
    {
        var pluginFiles = Directory.GetFiles("Plugins", "*.dll");
        foreach (var pluginFile in pluginFiles)
        {
            var assembly = Assembly.LoadFrom(pluginFile);
            var pluginTypes = assembly.GetTypes().Where(t => t.Name.Contains("SecurityTokenPlugin"));
            foreach (var pluginType in pluginTypes)
            {
                var registerMethod = pluginType.GetMethod("RegisterServices");
                registerMethod?.Invoke(null, new object[] { services });
            }
        }
    }
}
