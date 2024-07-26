using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace SecurityTokenService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Contains("--g-aes-key"))
            {
                using Aes aes = Aes.Create();
                aes.KeySize = 128; // 可以设置为 128、192 或 256 位
                aes.GenerateKey();
                Console.WriteLine("生成的 AES 密钥: " + Convert.ToBase64String(aes.Key));
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            CreateHostBuilder(args).Build().Run();
        }

        internal static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, builder) =>
                {
                    // if (File.Exists("appsettings.Nacos.json"))
                    // {
                    //     builder.AddJsonFile("appsettings.Nacos.json", true, true);
                    // }

                    var configuration = builder.Build();

                    var serilogSection = configuration.GetSection("Serilog");
                    if (serilogSection.GetChildren().Any())
                    {
                        Log.Logger = new LoggerConfiguration().ReadFrom
                            .Configuration(configuration)
                            .CreateLogger();
                    }
                    else
                    {
                        var logFile = Environment.GetEnvironmentVariable("LOG");
                        if (string.IsNullOrEmpty(logFile))
                        {
                            logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/sts.log");
                        }

                        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                            .MinimumLevel.Override("System", LogEventLevel.Warning)
                            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Warning)
                            .Enrich.FromLogContext()
#if DEBUG
                            .WriteTo.Console()
#endif
                            .WriteTo.Async(x => x.File(logFile, rollingInterval: RollingInterval.Day))
                            .CreateLogger();
                    }

                    var path = "sts.json";
                    if (File.Exists(path))
                    {
                        builder.AddJsonFile(path, true, true);
                    }

                    // var nacosSection = configuration.GetSection("Nacos");
                    // if (nacosSection.GetChildren().Any())
                    // {
                    //     builder.AddNacosV2Configuration(nacosSection);
                    // }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // webBuilder.ConfigureKestrel(serverOptions =>
                    // {
                    //     serverOptions.Listen(IPAddress.Any, 80);
                    //
                    //     var certPath = Environment.GetEnvironmentVariable("X509Certificate2");
                    //     if (string.IsNullOrWhiteSpace(certPath))
                    //     {
                    //         return;
                    //     }
                    //
                    //     var privateKeyPath = Path.GetFileNameWithoutExtension(certPath) + ".key";
                    //     var cert = CreateX509Certificate2(certPath, privateKeyPath);
                    //
                    //     serverOptions.Listen(IPAddress.Any, 8100,
                    //         (Action<ListenOptions>)(listenOptions => listenOptions.UseHttps(cert)));
                    // });
                    webBuilder.UseStartup<Startup>();
                });

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
}
