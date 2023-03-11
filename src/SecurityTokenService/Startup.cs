using System;
using System.IO;
using System.Reflection;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
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
using SecurityTokenService.IdentityServer;
using SecurityTokenService.Options;
using SecurityTokenService.Sms;
using SecurityTokenService.Stores;

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

            // 影响隐私数据加密、AntiToken 加解密
            services.AddDataProtection().SetApplicationName("SecurityTokenService")
                .PersistKeysToFileSystem(keysFolder)
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddScoped<SeedData>();

            services.AddCors(option => option
                .AddPolicy("cors", policy =>
                    policy.AllowAnyMethod()
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowCredentials()
                ));

            // 注册短信平台
            switch (Configuration["SecurityTokenService:SmsProvider"])
            {
                case "TencentCloud":
                    services.AddTransient<ISmsSender, TencentCloudSmsSender>();
                    break;
                default:
                    services.AddTransient<ISmsSender, AliyunSmsSender>();
                    break;
            }

            ConfigureDbContext(services);
            ConfigureIdentity(services);
            ConfigureIdentityServer(services);
            ConfigureOptions(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            IdentitySeedData.Load(app);

            app.MigrateIdentityServer();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // app.UseExceptionHandler("/Error");
                var htmlFiles = Directory.GetFiles("wwwroot", "*.html");
                foreach (var htmlFile in htmlFiles)
                {
                    var html = File.ReadAllText(htmlFile);
                    if (html.Contains("site.js"))
                    {
                        File.WriteAllText(htmlFile, html.Replace("site.js", "site.min.js"));
                    }
                }
            }

            if (Configuration["IdentityServer:ForceHttps"]?.ToLower() == "true")
            {
                app.UseMiddleware<PublicFacingUrlMiddleware>();
            }

            app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });
            app.UseFileServer();
            app.UseRouting();
            app.UseCors("cors");
            app.UseIdentityServer();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireCors("cors");
            });
        }

        // private string[] GetCorsOrigins()
        // {
        //     return Configuration.GetSection("AllowedCorsOrigins").Get<string[]>() ?? Array.Empty<string>();
        // }

        private void ConfigureIdentity(IServiceCollection services)
        {
            var identityBuilder = services.AddIdentity<User, IdentityRole>();
            identityBuilder.AddDefaultTokenProviders()
                .AddErrorDescriber<SecurityTokenServiceIdentityErrorDescriber>();

            if (Configuration.GetDatabaseType() == "MySql")
            {
                identityBuilder.AddEntityFrameworkStores<MySqlSecurityTokenServiceDbContext>();
            }
            else
            {
                identityBuilder.AddEntityFrameworkStores<PostgreSqlSecurityTokenServiceDbContext>();
            }
        }

        private void ConfigureIdentityServer(IServiceCollection services)
        {
            // IdentityServer4.Endpoints.TokenEndpoint
            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;

                    // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                    options.EmitStaticAudienceClaim = true;
                })
                .AddExtensionGrantValidator<PhoneCodeGrantValidator>()
                .AddStore(Configuration)
                .AddAspNetIdentity<User>()
                .AddProfileService<ProfileService>()
                .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>();

            services.AddScoped<IPhoneCodeStore, PhoneCodeStore>();

            if (Configuration.GetDatabaseType() == "MySql")
            {
                builder.AddOperationalStore<MySqlPersistedGrantDbContext>();
            }
            else if (Configuration.GetDatabaseType() == "Postgre")
            {
                builder.AddOperationalStore<PostgreSqlPersistedGrantDbContext>();
            }
            else
            {
                throw new NotSupportedException("不支持的数据库类型");
            }

            // not recommended for production - you need to store your key material somewhere secure
            // https 证书？
            builder.AddDeveloperSigningCredential();
        }

        private void ConfigureDbContext(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("Identity");
            if (Configuration.GetDatabaseType() == "MySql")
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
                    b.UseMySql(Configuration.GetConnectionString("IdentityServer"),
                        ServerVersion.AutoDetect(connectionString),
                        o =>
                        {
                            o.MigrationsAssembly(GetType().GetTypeInfo().Assembly.GetName().Name);
                            o.MigrationsHistoryTable("identity_server_migrations_history");
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
                            o.MigrationsHistoryTable("identity_server_migrations_history");
                        });
                });
            }
        }

        private void ConfigureOptions(IServiceCollection services)
        {
            var identity = Configuration.GetSection("Identity");
            services.Configure<ResourcesAndClientsOptions>(Configuration);
            services.Configure<IdentityOptions>(identity);
            services.Configure<IdentityExtensionOptions>(identity);
            services.Configure<IdentityServerOptions>(Configuration.GetSection("IdentityServer"));
            services.Configure<IdentityServerExtensionOptions>(Configuration.GetSection("IdentityServer"));
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
            services.Configure<AliyunOptions>(Configuration.GetSection("Aliyun"));
        }
    }
}
