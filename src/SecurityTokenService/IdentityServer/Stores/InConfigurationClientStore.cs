using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Configuration;

namespace SecurityTokenService.IdentityServer.Stores;

public class InConfigurationClientStore : IClientStore
{
    private readonly IConfiguration _configuration;

    public InConfigurationClientStore(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Client> FindClientByIdAsync(string clientId)
    {
        var clients = _configuration.GetSection("clients").Get<List<Client>>() ?? Enumerable.Empty<Client>();
        return Task.FromResult(clients
            .Where((Func<Client, bool>)(client => client.ClientId == clientId)).SingleOrDefault());
    }
}
