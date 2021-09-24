using System;
using System.Collections.Generic;
using System.IO;
using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Extensions
{
    public static class IdentityServerBuilderExtensions
    {
        class Config
        {
            public List<ApiScope> ApiScopes { get; set; }

            // ReSharper disable once CollectionNeverUpdated.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<ApiResource> ApiResources { get; set; }

            // ReSharper disable once CollectionNeverUpdated.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<Client> Clients { get; set; }

            // ReSharper disable once CollectionNeverUpdated.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<IdentityResource> IdentityResources { get; set; }
        }

        private static class Default
        {
            public static IEnumerable<IdentityResource> Ids =>
                new List<IdentityResource>
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                };

            public static IEnumerable<ApiResource> Apis =>
                new List<ApiResource>
                {
                    new ApiResource("api1", "My API")
                };

            public static IEnumerable<Client> Clients =>
                new List<Client>
                {
                    // machine to machine client
                    new Client
                    {
                        ClientId = "client",
                        ClientSecrets = { new Secret("secret".Sha256()) },

                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        // scopes that client has access to
                        AllowedScopes = { "api1" }
                    },
                    // interactive ASP.NET Core MVC client
                    new Client
                    {
                        ClientId = "mvc",
                        ClientSecrets = { new Secret("secret".Sha256()) },

                        AllowedGrantTypes = GrantTypes.Code,
                        RequireConsent = false,
                        RequirePkce = true,

                        // where to redirect to after login
                        RedirectUris = { "http://localhost:8002/signin-oidc" },

                        // where to redirect to after logout
                        PostLogoutRedirectUris = { "http://localhost:8002/signout-callback-oidc" },

                        AllowedScopes = new List<string>
                        {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Profile,
                            "api1"
                        },

                        AllowOfflineAccess = true
                    },
                    // JavaScript Client
                    new Client
                    {
                        ClientId = "js",
                        ClientName = "JavaScript Client",
                        AllowedGrantTypes = GrantTypes.Code,
                        RequirePkce = true,
                        RequireClientSecret = false,
                        RequireConsent = true,
                        RedirectUris = { "http://localhost:8003/callback.html" },
                        PostLogoutRedirectUris = { "http://localhost:8003/index.html" },
                        AllowedCorsOrigins = { "http://localhost:8003" },
                        AllowedScopes =
                        {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Profile,
                            "api1"
                        }
                    }
                };

            public static IEnumerable<ApiScope> ApiScopes => new List<ApiScope>
            {
                new ApiScope("api1", "My API1")
            };
        }

        public static IIdentityServerBuilder AddJsonConfig(this IIdentityServerBuilder builder)
        {
            var path = "sts.json";
            if (File.Exists(path))
            {
                var json = File.ReadAllText("sts.json");
                var config = JsonConvert.DeserializeObject<Config>(json);
                builder.AddInMemoryIdentityResources(config.IdentityResources)
                    .AddInMemoryApiScopes(config.ApiScopes)
                    .AddInMemoryApiResources(config.ApiResources)
                    .AddInMemoryClients(config.Clients);
            }
            else
            {
                builder.AddInMemoryIdentityResources(Default.Ids)
                    .AddInMemoryApiScopes(Default.ApiScopes)
                    .AddInMemoryApiResources(Default.Apis)
                    .AddInMemoryClients(Default.Clients)
                    .AddTestUsers(TestUsers.Users);
            }

            return builder;
        }
    }
}