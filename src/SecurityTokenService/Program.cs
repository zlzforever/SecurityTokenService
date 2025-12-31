using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Identity.Sm;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecurityTokenService.Data;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;
using SecurityTokenService.IdentityServer;
using SecurityTokenService.Middlewares;
using Serilog;

namespace SecurityTokenService;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        GenerateAesKey(args);

        var app = CreateApp(args);

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
        IdentitySeedData.Load(app);
        app.MigrateIdentityServer();
        if (!app.Environment.IsDevelopment())
        {
            var htmlFiles = Directory.GetFiles("wwwroot", "*.html");
            foreach (var htmlFile in htmlFiles)
            {
                var html = await File.ReadAllTextAsync(htmlFile);
                if (html.Contains("site.js"))
                {
                    await File.WriteAllTextAsync(htmlFile,
                        html.Replace("site.js", $"site.min.js?_t={DateTimeOffset.Now.ToUnixTimeSeconds()}"));
                }
            }

            logger.LogInformation("处理 js 引用完成");
        }

        var path = app.Configuration["BasePath"] ?? app.Configuration["PathBase"] ?? app.Configuration["PATH_BASE"];
        if (!string.IsNullOrEmpty(path))
        {
            app.UsePathBase(path);
        }

        // 解密请求体
        app.UseMiddleware<DecryptRequestMiddleware>();
        app.UseHealthChecks("/healthz");
        app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });
        app.UseFileServer();
        // app.UseRateLimiter();
        app.UseRouting();
        app.UseCors("cors");
        app.UseMiddleware<PublicFacingUrlMiddleware>(app.Configuration);
        app.UseIdentityServer();
        app.UseAuthorization();
        var inDapr = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DAPR_HTTP_PORT"));
        if (inDapr)
        {
            app.UseCloudEvents();
            app.MapSubscribeHandler();
        }

        app.MapControllers().RequireCors("cors");
        app.UsePlugins();
        await app.RunAsync();
        Console.WriteLine("Bye!");
    }

    internal static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddSerilog();
        builder.Configuration.AddEnvironmentVariables();
        var dataPath = builder.Configuration["DATAPATH"] ?? builder.Configuration["DATA_PATH"] ?? "sts.json";
        if (File.Exists(dataPath))
        {
            builder.Configuration.AddJsonFile(dataPath, true, true);
        }

        var mvcBuilder = builder.Services.AddControllers();
        var daprHttpPort = builder.Configuration["DaprHttpPort"] ?? builder.Configuration["DAPR_HTTP_PORT"];
        if (!string.IsNullOrWhiteSpace(daprHttpPort))
        {
            mvcBuilder.AddDapr();
        }

        var enableDataProtection = builder.Configuration["DATA_PROTECTION_ENABLE"] ??
                                   builder.Configuration["DataProtection:Enable"];
        if ("true".Equals(enableDataProtection, StringComparison.OrdinalIgnoreCase))
        {
            builder.AddDataProtection();
        }

        builder.AddSmsSender();
        builder.AddDbContext();
        builder.AddIdentity();
        builder.AddIdentityServer();
        if (bool.TryParse(builder.Configuration["ENABLE_SM3_PASSWORD_HASHER"],
                out var enable) &&
            enable)
        {
            builder.Services.AddSm3PasswordHasher<User>();
            builder.Services.AddSm3PasswordHasher<IdentityUser>();
        }

        builder.ConfigureOptions();
        builder.Services.AddScoped<SeedData>();

        builder.Services.AddRouting(options => options.LowercaseUrls = true);
        builder.Services.AddHealthChecks();
        builder.Services.AddCors(option => option
            .AddPolicy("cors", policy =>
                policy.AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowCredentials()
            ));
        builder.Host.UseSerilog();
        builder.LoadPlugins();

        var app = builder.Build();
        return app;
    }

    private static void GenerateAesKey(string[] args)
    {
        if (args.Contains("--g-aes-key"))
        {
            Console.WriteLine("生成的 AES 密钥: " + Utils.Util.CreateAesKey());

            // var origin = "17Th3f67y1rqTgC0AQqGag==";
            // var ad     = "17Th3f67y1zzzzzzrqTgC0AQqGag=="
            // var b = DecryptRequestMiddleware.GetRealKey(adjust);
            // var r = origin == b;
        }
    }
}
