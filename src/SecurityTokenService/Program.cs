using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace SecurityTokenService
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var logFile = Environment.GetEnvironmentVariable("LOG");
            if (string.IsNullOrEmpty(logFile))
            {
                logFile = Path.Combine(AppContext.BaseDirectory, "logs/sts.log");
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.RollingFile(logFile,
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            try
            {
                Log.Information("Version 1.0.1");
                Log.Information("Starting host...");
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        internal static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var path = "sts.json";
                    if (File.Exists(path))
                    {
                        builder.AddJsonFile(path);
                    }

                    var nacos = context.Configuration.GetSection("Nacos");
                    if (nacos.GetChildren().Any())
                    {
                        builder.AddNacosV2Configuration(nacos);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.Listen(IPAddress.Any, 8099);

                        var certPath = Environment.GetEnvironmentVariable("X509Certificate2");
                        if (string.IsNullOrWhiteSpace(certPath))
                        {
                            return;
                        }

                        var privateKeyPath = Path.GetFileNameWithoutExtension(certPath) + ".key";
                        var cert = CreateX509Certificate2(certPath, privateKeyPath);

                        serverOptions.Listen(IPAddress.Any, 8100,
                            (Action<ListenOptions>)(listenOptions => listenOptions.UseHttps(cert)));
                    });
                    webBuilder.UseStartup<Startup>();
                });

        private static X509Certificate2 CreateX509Certificate2(
            string certificatePath,
            string privateKeyPath)
        {
            using var certificate = new X509Certificate2(certificatePath);
            var strArray = File.ReadAllText(privateKeyPath).Split("-", StringSplitOptions.RemoveEmptyEntries);
            var source = Convert.FromBase64String(strArray[1]);
            using var privateKey = RSA.Create();
            int bytesRead;
            switch (strArray[0])
            {
                case "BEGIN PRIVATE KEY":
                    privateKey.ImportPkcs8PrivateKey((ReadOnlySpan<byte>)source, out bytesRead);
                    break;
                case "BEGIN RSA PRIVATE KEY":
                    privateKey.ImportRSAPrivateKey((ReadOnlySpan<byte>)source, out bytesRead);
                    break;
            }
            return new X509Certificate2(certificate.CopyWithPrivateKey(privateKey).Export(X509ContentType.Pfx));
        }
    }
}