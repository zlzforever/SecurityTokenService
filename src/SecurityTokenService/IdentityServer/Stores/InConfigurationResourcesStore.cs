using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Configuration;

namespace SecurityTokenService.IdentityServer.Stores;

public class InConfigurationResourcesStore : IResourceStore
{
    private readonly IConfiguration _configuration;

    public InConfigurationResourcesStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        if (scopeNames == null) throw new ArgumentNullException(nameof(scopeNames));

        var identityResources = _configuration.GetSection("identityResources").Get<List<IdentityResource>>() ??
                                Enumerable.Empty<IdentityResource>();
        var identity = from i in identityResources
            where scopeNames.Contains(i.Name)
            select i;

        return Task.FromResult(identity);
    }

    public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        if (scopeNames == null) throw new ArgumentNullException(nameof(scopeNames));

        var apiScopes = _configuration.GetSection("apiScopes").Get<List<ApiScope>>() ??
                        Enumerable.Empty<ApiScope>();
        var query =
            from x in apiScopes
            where scopeNames.Contains(x.Name)
            select x;

        return Task.FromResult(query);
    }

    public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        if (scopeNames == null) throw new ArgumentNullException(nameof(scopeNames));

        var apiResources = _configuration.GetSection("apiResources").Get<List<ApiResource>>() ??
                           Enumerable.Empty<ApiResource>();

        var query = from a in apiResources
            where a.Scopes.Any(scopeNames.Contains)
            select a;

        return Task.FromResult(query);
    }

    public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        if (apiResourceNames == null)
        {
            throw new ArgumentNullException(nameof(apiResourceNames));
        }

        var apiResources = _configuration.GetSection("apiResources").Get<List<ApiResource>>() ??
                           Enumerable.Empty<ApiResource>();

        var query = from a in apiResources
            where apiResourceNames.Contains(a.Name)
            select a;
        return Task.FromResult(query);
    }

    public Task<Resources> GetAllResourcesAsync()
    {
        var identityResources = _configuration.GetSection("identityResources").Get<List<IdentityResource>>() ??
                                Enumerable.Empty<IdentityResource>();
        var apiScopes = _configuration.GetSection("apiScopes").Get<List<ApiScope>>() ??
                        Enumerable.Empty<ApiScope>();
        var apiResources = _configuration.GetSection("apiResources").Get<List<ApiResource>>() ??
                           Enumerable.Empty<ApiResource>();
        var result = new Resources(identityResources, apiResources, apiScopes);
        return Task.FromResult(result);
    }
}
