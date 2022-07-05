using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Options;

namespace SecurityTokenService.IdentityServer.Stores;

public class InConfigurationClientStore : IClientStore
{
    private readonly ResourcesAndClientsOptions _options;

    public InConfigurationClientStore(IOptionsMonitor<ResourcesAndClientsOptions> options)
    {
        _options = options.CurrentValue;
    }

    public Task<Client> FindClientByIdAsync(string clientId)
    {
        var client = _options.Clients.SingleOrDefault(client => client.ClientId == clientId);
        return Task.FromResult(client);
    }
}
