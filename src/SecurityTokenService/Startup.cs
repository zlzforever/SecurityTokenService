using System;
using System.IO;
using System.Linq;
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
using Microsoft.Extensions.Options;
using SecurityTokenService.Data;
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
            services.AddControllers();

            services.AddDbContextPool<SecurityTokenServiceDbContext>(b =>
            {
                b.UseNpgsql(Configuration.GetConnectionString("Identity"),
                    o =>
                    {
                        o.MigrationsAssembly(GetType().GetTypeInfo().Assembly.GetName().Name);
                        o.MigrationsHistoryTable("_identity_migrations_history");
                    });
            });
            services.AddDbContext<PersistedGrantDbContext>(b =>
            {
                b.UseNpgsql(Configuration.GetConnectionString("IdentityServer"),
                    o =>
                    {
                        o.MigrationsAssembly(GetType().GetTypeInfo().Assembly.GetName().Name);
                        o.MigrationsHistoryTable("_identity_server_migrations_history");
                    });
            });

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddDefaultTokenProviders()
                .AddErrorDescriber<SecurityTokenServiceIdentityErrorDescriber>()
                .AddEntityFrameworkStores<SecurityTokenServiceDbContext>();

            var keysFolder = new DirectoryInfo("KEYS");
            if (!keysFolder.Exists)
            {
                keysFolder.Create();
            }

            // 影响隐私数据加密、AntiToken 加解密
            services.AddDataProtection().SetApplicationName("SecurityTokenService")
                .PersistKeysToFileSystem(keysFolder)
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

            var builder = services.AddIdentityServer()
                    .AddJsonConfig()
                    .AddOperationalStore<PersistedGrantDbContext>()
                    .AddAspNetIdentity<IdentityUser>()
                ;

            // not recommended for production - you need to store your key material somewhere secure
            // https 证书？
            builder.AddDeveloperSigningCredential();

            services.AddAntiforgery(x =>
            {
                x.Cookie.Name = "X-CSRF-TOKEN";
                x.HeaderName = "X-CSRF-TOKEN";
                x.FormFieldName = "__AntiforgeryToken";
                x.SuppressXFrameOptionsHeader = false;
            });

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
            using var scope = app.ApplicationServices.CreateScope();

            var persistedGrantDbContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();

            persistedGrantDbContext.Database.Migrate();

            if (string.Equals(Configuration["Identity:SelfHost"], "true", StringComparison.InvariantCultureIgnoreCase))
            {
                var securityTokenServiceDbContext =
                    scope.ServiceProvider.GetRequiredService<SecurityTokenServiceDbContext>();
                securityTokenServiceDbContext.Database.Migrate();
            }

            var seedData = scope.ServiceProvider.GetRequiredService<SeedData>();
            seedData.Load();

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
    }
}