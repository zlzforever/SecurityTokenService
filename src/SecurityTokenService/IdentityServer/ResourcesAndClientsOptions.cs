using System.Collections.Generic;
using IdentityServer4.Models;

namespace SecurityTokenService.IdentityServer;

public class ResourcesAndClientsOptions
{
    public List<Client> Clients { get; set; }
    public List<IdentityResource> IdentityResources { get; set; }
    public List<ApiScope> ApiScopes { get; set; }
    public List<ApiResource> ApiResources { get; set; }
}
