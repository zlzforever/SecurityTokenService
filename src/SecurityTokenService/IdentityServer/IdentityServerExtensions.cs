using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IdentityServer4;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecurityTokenService.Data.MySql;
using SecurityTokenService.Data.PostgreSql;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;
using SecurityTokenService.IdentityServer.Stores;

namespace SecurityTokenService.IdentityServer
{
    public static class IdentityServerExtensions
    {
        /// <summary>
        /// 重写 UseIdentityServer 的原因是默认添加了 BaseUrlMiddleware
        /// 其会调用 SetIdentityServerBasePath 设置系统的 BasePath
        /// 暂时没有好的办法移除此中间件的注册
        /// 又不能直接把 Request.BasePath 进行全局修改（会影响其它功能）
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseIdentityServer(this IApplicationBuilder app,
            IConfiguration configuration,
            IdentityServerMiddlewareOptions options = null)
        {
            var method = typeof(IdentityServerApplicationBuilderExtensions)
                .GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static);
            method?.Invoke(null, [app]);

            app.UseMiddleware<PublicFacingUrlMiddleware>(configuration);

            app.ConfigureCors();

            // it seems ok if we have UseAuthentication more than once in the pipeline --
            // this will just re-run the various callback handlers and the default authN 
            // handler, which just re-assigns the user on the context. claims transformation
            // will run twice, since that's not cached (whereas the authN handler result is)
            // related: https://github.com/aspnet/Security/issues/1399
            if (options == null) options = new IdentityServerMiddlewareOptions();
            options.AuthenticationMiddleware(app);

            app.UseMiddleware<MutualTlsEndpointMiddleware>();
            app.UseMiddleware<IdentityServerMiddleware>();

            return app;
        }

        // class Config
        // {
        //     public List<ApiScope> ApiScopes { get; set; }
        //
        //     // ReSharper disable once CollectionNeverUpdated.Local
        //     // ReSharper disable once UnusedAutoPropertyAccessor.Local
        //     public List<ApiResource> ApiResources { get; set; }
        //
        //     // ReSharper disable once CollectionNeverUpdated.Local
        //     // ReSharper disable once UnusedAutoPropertyAccessor.Local
        //     public List<Client> Clients { get; set; }
        //
        //     // ReSharper disable once CollectionNeverUpdated.Local
        //     // ReSharper disable once UnusedAutoPropertyAccessor.Local
        //     public List<IdentityResource> IdentityResources { get; set; }
        // }

        private static class Default
        {
            public static IEnumerable<IdentityResource> Ids =>
                new List<IdentityResource> { new IdentityResources.OpenId(), new IdentityResources.Profile(), };

            public static IEnumerable<ApiResource> Apis =>
                new List<ApiResource> { new ApiResource("api1", "My API") };

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

            public static IEnumerable<ApiScope> ApiScopes => new List<ApiScope> { new ApiScope("api1", "My API1") };
        }

        public static IIdentityServerBuilder AddStore(this IIdentityServerBuilder builder,
            IConfiguration configuration)
        {
            if (configuration.GetSection("identityResources").GetChildren().Any())
            {
                Console.WriteLine("Load config from configuration");
                // var ids = configuration.GetSection("identityResources").Get<List<IdentityResource>>() ??
                //           new List<IdentityResource>();
                // var apiScopes = configuration.GetSection("apiScopes").Get<List<ApiScope>>() ?? new List<ApiScope>();
                // var apiResources = configuration.GetSection("apiResources").Get<List<ApiResource>>() ??
                //                    new List<ApiResource>();
                // var clients = configuration.GetSection("clients").Get<List<Client>>() ?? new List<Client>();
                // builder.AddInMemoryIdentityResources(ids)
                //     .AddInMemoryApiScopes(apiScopes)
                //     .AddInMemoryApiResources(apiResources)
                // .AddInMemoryClients(clients);
                builder.AddInMemoryCaching();
                builder.AddResourceStoreCache<InConfigurationResourcesStore>();
                builder.AddClientStoreCache<InConfigurationClientStore>();
            }
            else
            {
                builder.AddInMemoryIdentityResources(Default.Ids)
                    .AddInMemoryApiScopes(Default.ApiScopes)
                    .AddInMemoryApiResources(Default.Apis)
                    .AddInMemoryClients(Default.Clients);
            }

#if DEBUG
            builder.AddTestUsers(TestUsers.Users);
#endif

            return builder;
        }

        public static void MigrateIdentityServer(this IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            using var scope = app.ApplicationServices.CreateScope();
            if (configuration.GetDatabaseType() == "MySql")
            {
                using var persistedGrantDbContext =
                    scope.ServiceProvider.GetRequiredService<MySqlPersistedGrantDbContext>();
                persistedGrantDbContext.Database.Migrate();
            }
            else
            {
                using var persistedGrantDbContext =
                    scope.ServiceProvider.GetRequiredService<PostgreSqlPersistedGrantDbContext>();
                persistedGrantDbContext.Database.Migrate();
            }
        }
    }
}
