using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace SecurityTokenService.Extensions;

public static class PluginsExtensions
{
    public static void LoadPlugins(this IHostApplicationBuilder builder)
    {
        var pluginFiles = Directory.GetFiles("Plugins", "*.dll");
        foreach (var pluginFile in pluginFiles)
        {
            var assembly = Assembly.LoadFrom(pluginFile);
            var pluginTypes = assembly.GetTypes().Where(t => t.Name.Contains("SecurityTokenPlugin"));
            foreach (var pluginType in pluginTypes)
            {
                var loadMethod = pluginType.GetMethod("Load");
                loadMethod?.Invoke(null, new object[] { builder });
            }
        }
    }
}
