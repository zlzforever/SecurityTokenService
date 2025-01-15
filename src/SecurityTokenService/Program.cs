using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecurityTokenService.Data;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;
using SecurityTokenService.IdentityServer;
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
    }

    internal static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddSerilog();

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
        // builder.Services.AddRateLimiter(b =>
        // {
        //     b.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        //     b.OnRejected = async (ctx, cancellationToken) =>
        //     {
        //         await ctx.HttpContext.Response.WriteAsync("访问过于频繁", cancellationToken);
        //     };
        //     b.AddSlidingWindowLimiter(policyName: "sliding", options =>
        //     {
        //         options.PermitLimit = 10;
        //         options.Window = TimeSpan.FromSeconds(60);
        //         options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        //         options.QueueLimit = 20;
        //     });
        // });
        builder.Host.UseSerilog();
        builder.LoadPlugins();

        var app = builder.Build();
        return app;
    }

    private static void GenerateAesKey(string[] args)
    {
        if (args.Contains("--g-aes-key"))
        {
            using Aes aes = Aes.Create();
            aes.KeySize = 128; // 可以设置为 128、192 或 256 位
            aes.GenerateKey();
            Console.WriteLine("生成的 AES 密钥: " + Convert.ToBase64String(aes.Key));
        }
    }

    // internal static IHostBuilder CreateHostBuilder(string[] args) =>
    //     Host.CreateDefaultBuilder(args)
    //         .ConfigureWebHostDefaults(webBuilder =>
    //         {
    //             // webBuilder.ConfigureKestrel(serverOptions =>
    //             // {
    //             //     serverOptions.Listen(IPAddress.Any, 80);
    //             //
    //             //     var certPath = Environment.GetEnvironmentVariable("X509Certificate2");
    //             //     if (string.IsNullOrWhiteSpace(certPath))
    //             //     {
    //             //         return;
    //             //     }
    //             //
    //             //     var privateKeyPath = Path.GetFileNameWithoutExtension(certPath) + ".key";
    //             //     var cert = CreateX509Certificate2(certPath, privateKeyPath);
    //             //
    //             //     serverOptions.Listen(IPAddress.Any, 8100,
    //             //         (Action<ListenOptions>)(listenOptions => listenOptions.UseHttps(cert)));
    //             // });
    //             webBuilder.UseStartup<Startup>();
    //         }).UseSerilog();

    // private static X509Certificate2 CreateX509Certificate2(
    //     string certificatePath,
    //     string privateKeyPath)
    // {
    //     using var certificate = new X509Certificate2(certificatePath);
    //     var strArray = File.ReadAllText(privateKeyPath).Split("-", StringSplitOptions.RemoveEmptyEntries);
    //     var source = Convert.FromBase64String(strArray[1]);
    //     using var privateKey = RSA.Create();
    //     int bytesRead;
    //     switch (strArray[0])
    //     {
    //         case "BEGIN PRIVATE KEY":
    //             privateKey.ImportPkcs8PrivateKey((ReadOnlySpan<byte>)source, out bytesRead);
    //             break;
    //         case "BEGIN RSA PRIVATE KEY":
    //             privateKey.ImportRSAPrivateKey((ReadOnlySpan<byte>)source, out bytesRead);
    //             break;
    //     }
    //
    //     return new X509Certificate2(certificate.CopyWithPrivateKey(privateKey).Export(X509ContentType.Pfx));
    // }
}
