using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using IdentityServer4;
using IdentityServer4.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecurityTokenService.Data.MySql;
using SecurityTokenService.Data.PostgreSql;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;
using SecurityTokenService.IdentityServer;
using SecurityTokenService.Options;
using SecurityTokenService.Sms;
using SecurityTokenService.Stores;
using Serilog;
using Serilog.Events;

namespace SecurityTokenService;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
    {
        var serilogSection = builder.Configuration.GetSection("Serilog");
        if (serilogSection.GetChildren().Any())
        {
            Log.Logger = new LoggerConfiguration().ReadFrom
                .Configuration(builder.Configuration)
                .CreateLogger();
        }
        else
        {
            var logPath = builder.Configuration["LOG_PATH"] ?? builder.Configuration["LOGPATH"];
            if (string.IsNullOrEmpty(logPath))
            {
                logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/log.txt");
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Async(x => x.File(logPath, rollingInterval: RollingInterval.Day))
                .CreateLogger();
        }

        return builder;
    }

    public static WebApplicationBuilder AddDbContext(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("Identity");
        if (builder.Configuration.GetDatabaseType() == "MySql")
        {
            builder.Services.AddDbContextPool<MySqlSecurityTokenServiceDbContext>(b =>
            {
                b.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                    o =>
                    {
                        o.MigrationsAssembly(typeof(WebApplicationBuilderExtensions).GetTypeInfo().Assembly.GetName()
                            .Name);
                        o.MigrationsHistoryTable("___identity_migrations_history");
                    });
            });
            builder.Services.AddDbContext<MySqlPersistedGrantDbContext>(b =>
            {
                b.UseMySql(builder.Configuration.GetConnectionString("IdentityServer"),
                    ServerVersion.AutoDetect(connectionString),
                    o =>
                    {
                        o.MigrationsAssembly(typeof(WebApplicationBuilderExtensions).GetTypeInfo().Assembly.GetName()
                            .Name);
                        o.MigrationsHistoryTable("identity_server_migrations_history");
                    });
            });
        }
        else
        {
            builder.Services.AddDbContextPool<PostgreSqlSecurityTokenServiceDbContext>(b =>
            {
                b.UseNpgsql(builder.Configuration.GetConnectionString("Identity"),
                    o =>
                    {
                        o.MigrationsAssembly(typeof(WebApplicationBuilderExtensions).GetTypeInfo().Assembly.GetName()
                            .Name);
                        o.MigrationsHistoryTable("___identity_migrations_history");
                    });
            });
            builder.Services.AddDbContext<PostgreSqlPersistedGrantDbContext>(b =>
            {
                b.UseNpgsql(builder.Configuration.GetConnectionString("IdentityServer"),
                    o =>
                    {
                        o.MigrationsAssembly(typeof(WebApplicationBuilderExtensions).GetTypeInfo().Assembly.GetName()
                            .Name);
                        o.MigrationsHistoryTable("identity_server_migrations_history");
                    });
            });
        }

        return builder;
    }

    public static WebApplicationBuilder AddDataProtection(this WebApplicationBuilder builder)
    {
        var dataProtectionKey = builder.Configuration["DataProtection:Key"];
        if (!string.IsNullOrEmpty(dataProtectionKey))
        {
            Util.DataProtectionKeyAes = System.Security.Cryptography.Aes.Create();
            var key = Encoding.UTF8.GetBytes(dataProtectionKey);
            if (Util.DataProtectionKeyAes.ValidKeySize(key.Length))
            {
                Util.DataProtectionKeyAes.Key = key;
            }
            else
            {
                Log.Logger.Error("DataProtectionKey 长度不正确");
                Environment.Exit(-1);
            }
        }

        // 影响隐私数据加密、AntiToken 加解密
        var dataProtectionBuilder = builder.Services.AddDataProtection()
            .SetApplicationName("SecurityTokenService")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        if (builder.Configuration.GetDatabaseType() == "MySql")
        {
            dataProtectionBuilder.PersistKeysToDbContext<MySqlSecurityTokenServiceDbContext>();
        }
        else
        {
            dataProtectionBuilder.PersistKeysToDbContext<PostgreSqlSecurityTokenServiceDbContext>();
        }

        return builder;
    }

    public static WebApplicationBuilder AddSmsSender(this WebApplicationBuilder builder)
    {
        // 注册短信平台
        switch (builder.Configuration["SecurityTokenService:SmsProvider"])
        {
            case "TencentCloud":
                builder.Services.AddTransient<ISmsSender, TencentCloudSmsSender>();
                break;
            default:
                builder.Services.AddTransient<ISmsSender, AliyunSmsSender>();
                break;
        }

        return builder;
    }

    public static WebApplicationBuilder AddIdentity(this WebApplicationBuilder builder)
    {
        var identityBuilder = builder.Services.AddIdentity<User, IdentityRole>();
        identityBuilder.AddDefaultTokenProviders()
            .AddErrorDescriber<SecurityTokenServiceIdentityErrorDescriber>();

        if (builder.Configuration.GetDatabaseType() == "MySql")
        {
            identityBuilder.AddEntityFrameworkStores<MySqlSecurityTokenServiceDbContext>();
        }
        else
        {
            identityBuilder.AddEntityFrameworkStores<PostgreSqlSecurityTokenServiceDbContext>();
        }

        return builder;
    }

    public static WebApplicationBuilder AddIdentityServer(this WebApplicationBuilder builder)
    {
        // IdentityServer4.Endpoints.TokenEndpoint
        var identityServerBuilder = builder.Services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = true;
            })
            .AddExtensionGrantValidator<PhoneCodeGrantValidator>()
            .AddStore(builder.Configuration)
            .AddAspNetIdentity<User>()
            .AddProfileService<ProfileService>()
            .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>();

        identityServerBuilder.Services.AddScoped<IPhoneCodeStore, PhoneCodeStore>();

        if (builder.Configuration.GetDatabaseType() == "MySql")
        {
            identityServerBuilder.AddOperationalStore<MySqlPersistedGrantDbContext>();
        }
        else if (builder.Configuration.GetDatabaseType() == "Postgre")
        {
            identityServerBuilder.AddOperationalStore<PostgreSqlPersistedGrantDbContext>();
        }
        else
        {
            throw new NotSupportedException("不支持的数据库类型");
        }

        // not recommended for production - you need to store your key material somewhere secure
        identityServerBuilder.AddDeveloperSigningCredential();

        return builder;
    }

    public static WebApplicationBuilder ConfigureOptions(this WebApplicationBuilder builder)
    {
        var identity = builder.Configuration.GetSection("Identity");
        builder.Services.Configure<ResourcesAndClientsOptions>(builder.Configuration);
        builder.Services.Configure<IdentityOptions>(identity);
        builder.Services.Configure<IdentityExtensionOptions>(identity);
        builder.Services.Configure<IdentityServerOptions>(builder.Configuration.GetSection("IdentityServer"));
        builder.Services.Configure<IdentityServerExtensionOptions>(builder.Configuration.GetSection("IdentityServer"));
        builder.Services.Configure<SecurityTokenServiceOptions>(
            builder.Configuration.GetSection("SecurityTokenService"));

        builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
            builder.Configuration.GetSection("ApplicationCookieAuthentication"));
        builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme,
            builder.Configuration.GetSection("ExternalCookieAuthentication"));
        builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme,
            builder.Configuration.GetSection("TwoFactorUserIdCookieAuthentication"));

        builder.Services.Configure<CookieAuthenticationOptions>(
            IdentityServerConstants.DefaultCookieAuthenticationScheme,
            builder.Configuration.GetSection("IdentityServerCookieAuthentication"));
        builder.Services.Configure<CookieAuthenticationOptions>(
            IdentityServerConstants.ExternalCookieAuthenticationScheme,
            builder.Configuration.GetSection("IdentityServerExternalCookieAuthentication"));
        builder.Services.Configure<CookieAuthenticationOptions>(IdentityServerConstants.DefaultCheckSessionCookieName,
            builder.Configuration.GetSection("IdentityServerCheckSessionCookieAuthentication"));
        builder.Services.Configure<AliyunOptions>(builder.Configuration.GetSection("Aliyun"));
        return builder;
    }
}
