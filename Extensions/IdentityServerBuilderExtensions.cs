using System;
using System.Collections.Generic;
using System.IO;
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
            // ReSharper disable once CollectionNeverUpdated.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<ApiScope> ApiScopes { get; set; }

            // ReSharper disable once CollectionNeverUpdated.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<Client> Clients { get; set; }

            // ReSharper disable once CollectionNeverUpdated.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<IdentityResource> IdentityResources { get; set; }
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
                    .AddInMemoryClients(config.Clients);
            }
            else
            {
                builder.AddInMemoryIdentityResources(Array.Empty<IdentityResource>())
                    .AddInMemoryApiScopes(Array.Empty<ApiScope>())
                    .AddInMemoryClients(Array.Empty<Client>())
                    .AddTestUsers(TestUsers.Users);
            }

            return builder;
        }
    }
}