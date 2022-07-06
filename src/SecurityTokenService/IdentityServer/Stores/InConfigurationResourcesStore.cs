using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Options;

namespace SecurityTokenService.IdentityServer.Stores;

public class InConfigurationResourcesStore : IResourceStore
{
    private readonly ResourcesAndClientsOptions _options;

    public InConfigurationResourcesStore(IOptionsMonitor<ResourcesAndClientsOptions> options)
    {
        _options = options.CurrentValue;
    }

    public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        if (scopeNames == null)
        {
            throw new ArgumentNullException(nameof(scopeNames));
        }

        if (_options.IdentityResources == null)
        {
            return Task.FromResult(Enumerable.Empty<IdentityResource>());
        }

        var identity = from i in _options.IdentityResources
            where scopeNames.Contains(i.Name)
            select i;

        return Task.FromResult(identity);
    }

    public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        if (scopeNames == null)
        {
            throw new ArgumentNullException(nameof(scopeNames));
        }

        if (_options.ApiScopes == null)
        {
            return Task.FromResult(Enumerable.Empty<ApiScope>());
        }

        var query =
            from x in _options.ApiScopes
            where scopeNames.Contains(x.Name)
            select x;

        return Task.FromResult(query);
    }

    public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        if (scopeNames == null)
        {
            throw new ArgumentNullException(nameof(scopeNames));
        }

        if (_options.ApiResources == null)
        {
            return Task.FromResult(Enumerable.Empty<ApiResource>());
        }

        var query = from a in _options.ApiResources
            where a.Scopes != null && a.Scopes.Any(scopeNames.Contains)
            select a;

        return Task.FromResult(query);
    }

    public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        if (apiResourceNames == null)
        {
            throw new ArgumentNullException(nameof(apiResourceNames));
        }

        if (_options.ApiResources == null)
        {
            return Task.FromResult(Enumerable.Empty<ApiResource>());
        }

        var query = from a in _options.ApiResources
            where apiResourceNames.Contains(a.Name)
            select a;
        return Task.FromResult(query);
    }

    public Task<Resources> GetAllResourcesAsync()
    {
        var result = new Resources(_options.IdentityResources, _options.ApiResources, _options.ApiScopes);
        return Task.FromResult(result);
    }
}
