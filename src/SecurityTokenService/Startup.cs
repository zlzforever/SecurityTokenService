using System;
using System.IO;
using System.Reflection;
using IdentityServer4;
using IdentityServer4.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurityTokenService.Data;
using SecurityTokenService.Data.MySql;
using SecurityTokenService.Data.PostgreSql;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;

namespace SecurityTokenService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var keysFolder = new DirectoryInfo("KEYS");
            if (!keysFolder.Exists)
            {
                keysFolder.Create();
            }

            services.AddControllers();

            ConfigureDbContext(services);

            var identityBuilder = services.AddIdentity<IdentityUser, IdentityRole>();
            identityBuilder.AddDefaultTokenProviders()
                .AddErrorDescriber<SecurityTokenServiceIdentityErrorDescriber>();

            if (Configuration["Database"] == "MySql")
            {
                identityBuilder.AddEntityFrameworkStores<MySqlSecurityTokenServiceDbContext>();
            }
            else
            {
                identityBuilder.AddEntityFrameworkStores<PostgreSqlSecurityTokenServiceDbContext>();
            }

            var builder = services.AddIdentityServer()
                .AddStore(Configuration)
                .AddAspNetIdentity<IdentityUser>();
            if (Configuration["Database"] == "MySql")
            {
                builder.AddOperationalStore<MySqlPersistedGrantDbContext>();
            }
            else
            {
                builder.AddOperationalStore<PostgreSqlPersistedGrantDbContext>();
            }

            // 影响隐私数据加密、AntiToken 加解密
            services.AddDataProtection().SetApplicationName("SecurityTokenService")
                .PersistKeysToFileSystem(keysFolder)
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            // not recommended for production - you need to store your key material somewhere secure
            // https 证书？
            builder.AddDeveloperSigningCredential();

            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddScoped<SeedData>();

            services.Configure<IdentityOptions>(Configuration.GetSection("Identity"));
            services.Configure<IdentityExtensionOptions>(Configuration.GetSection("Identity"));
            services.Configure<IdentityServerOptions>(Configuration.GetSection("IdentityServer"));
            services.Configure<SecurityTokenServiceOptions>(Configuration.GetSection("SecurityTokenService"));

            services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
                Configuration.GetSection("ApplicationCookieAuthentication"));
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme,
                Configuration.GetSection("ExternalCookieAuthentication"));
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme,
                Configuration.GetSection("TwoFactorUserIdCookieAuthentication"));

            services.Configure<CookieAuthenticationOptions>(IdentityServerConstants.DefaultCookieAuthenticationScheme,
                Configuration.GetSection("IdentityServerCookieAuthentication"));
            services.Configure<CookieAuthenticationOptions>(IdentityServerConstants.ExternalCookieAuthenticationScheme,
                Configuration.GetSection("IdentityServerExternalCookieAuthentication"));
            services.Configure<CookieAuthenticationOptions>(IdentityServerConstants.DefaultCheckSessionCookieName,
                Configuration.GetSection("IdentityServerCheckSessionCookieAuthentication"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.LoadIdentityData();
            app.LoadIdentityServerData();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.Lax
            });
            app.UseHttpsRedirection();
            app.UseFileServer();
            app.UseRouting();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        private void ConfigureDbContext(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("Identity");
            if (Configuration["Database"] == "MySql")
            {
                services.AddDbContextPool<MySqlSecurityTokenServiceDbContext>(b =>
                {
                    b.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                        o =>
                        {
                            o.MigrationsAssembly(GetType().GetTypeInfo().Assembly.GetName().Name);
                            o.MigrationsHistoryTable("___identity_migrations_history");
                        });
                });
                services.AddDbContext<MySqlPersistedGrantDbContext>(b =>
                {
                    b.UseNpgsql(Configuration.GetConnectionString("IdentityServer"),
                        o =>
                        {
                            o.MigrationsAssembly(GetType().GetTypeInfo().Assembly.GetName().Name);
                            o.MigrationsHistoryTable("___identity_server_migrations_history");
                        });
                });
            }
            else
            {
                services.AddDbContextPool<PostgreSqlSecurityTokenServiceDbContext>(b =>
                {
                    b.UseNpgsql(Configuration.GetConnectionString("Identity"),
                        o =>
                        {
                            o.MigrationsAssembly(GetType().GetTypeInfo().Assembly.GetName().Name);
                            o.MigrationsHistoryTable("___identity_migrations_history");
                        });
                });
                services.AddDbContext<PostgreSqlPersistedGrantDbContext>(b =>
                {
                    b.UseNpgsql(Configuration.GetConnectionString("IdentityServer"),
                        o =>
                        {
                            o.MigrationsAssembly(GetType().GetTypeInfo().Assembly.GetName().Name);
                            o.MigrationsHistoryTable("___identity_server_migrations_history");
                        });
                });
            }
        }
    }
}