using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace SecurityTokenService.Extensions;

public static class PluginsExtensions
{
    private static readonly List<Type> _pluginTypes;

    static PluginsExtensions()
    {
        _pluginTypes = new();
        var pluginFiles = Directory.GetFiles("Plugins", "*.dll");
        foreach (var pluginFile in pluginFiles)
        {
            var assembly = Assembly.LoadFrom(pluginFile);
            var pluginTypes = assembly.GetTypes().Where(t => t.Name.Contains("SecurityTokenPlugin"));
            if (pluginTypes.Any())
            {
                _pluginTypes.AddRange(pluginTypes);
            }
        }
    }

    public static void LoadPlugins(this IHostApplicationBuilder builder)
    {
        foreach (var pluginType in _pluginTypes)
        {
            var loadMethod = pluginType.GetMethod("Load");
            loadMethod?.Invoke(null, new object[] { builder });
        }
    }

    public static void UsePlugins(this WebApplication app)
    {
        foreach (var pluginType in _pluginTypes)
        {
            var loadMethod = pluginType.GetMethod("Use");
            loadMethod?.Invoke(null, new object[] { app });
        }
    }
}
