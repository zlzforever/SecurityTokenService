// using System;
// using System.IO;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using SecurityTokenService.Identity;
// using SecurityTokenService.IdentityServer;
//
// namespace SecurityTokenService;
//
// public class Startup
// {
//     public Startup(IConfiguration configuration)
//     {
//         Configuration = configuration;
//     }
//
//     public IConfiguration Configuration { get; }
//
//     // This method gets called by the runtime. Use this method to add services to the container.
//     public void ConfigureServices(IServiceCollection services)
//     {
//         // var keysFolder = new DirectoryInfo("KEYS");
//         // if (!keysFolder.Exists)
//         // {
//         //     keysFolder.Create();
//         // }
//         // comments by lewis at 20240222
//         // 必须是 128、256 位
//
//         // var dataProtectionKey = Configuration["DataProtection:Key"];
//         // if (!string.IsNullOrEmpty(dataProtectionKey))
//         // {
//         //     Util.DataProtectionKeyAes.Key = Encoding.UTF8.GetBytes(dataProtectionKey);
//         // }
//
//         // var builder = services.AddControllers();
//         // if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DAPR_HTTP_PORT")))
//         // {
//         //     builder.AddDapr();
//         // }
//
//         // // 影响隐私数据加密、AntiToken 加解密
//         // var dataProtectionBuilder = services.AddDataProtection()
//         //     .SetApplicationName("SecurityTokenService")
//         //     .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
//
//         // if (Configuration.GetDatabaseType() == "MySql")
//         // {
//         //     dataProtectionBuilder.PersistKeysToDbContext<MySqlSecurityTokenServiceDbContext>();
//         // }
//         // else
//         // {
//         //     dataProtectionBuilder.PersistKeysToDbContext<PostgreSqlSecurityTokenServiceDbContext>();
//         // }
//
//         // services.AddRouting(options => options.LowercaseUrls = true);
//         // services.AddHealthChecks();
//         // services.AddScoped<SeedData>();
//         //
//         // services.AddCors(option => option
//         //     .AddPolicy("cors", policy =>
//         //         policy.AllowAnyMethod()
//         //             .SetIsOriginAllowed(_ => true)
//         //             .AllowAnyHeader()
//         //             .AllowCredentials()
//         //     ));
//
//         // // 注册短信平台
//         // switch (Configuration["SecurityTokenService:SmsProvider"])
//         // {
//         //     case "TencentCloud":
//         //         services.AddTransient<ISmsSender, TencentCloudSmsSender>();
//         //         break;
//         //     default:
//         //         services.AddTransient<ISmsSender, AliyunSmsSender>();
//         //         break;
//         // }
//
//         // ConfigureDbContext(services);
//         // ConfigureIdentity(services);
//         // ConfigureIdentityServer(services);
//         // ConfigureOptions(services);
//     }
//
//     // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
//     public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//     {
//         var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
//         IdentitySeedData.Load(app);
//
//         app.MigrateIdentityServer();
//
//         if (env.IsDevelopment())
//         {
//             app.UseDeveloperExceptionPage();
//         }
//         else
//         {
//             // app.UseExceptionHandler("/Error");
//             var htmlFiles = Directory.GetFiles("wwwroot", "*.html");
//             foreach (var htmlFile in htmlFiles)
//             {
//                 var html = File.ReadAllText(htmlFile);
//                 if (html.Contains("site.js"))
//                 {
//                     File.WriteAllText(htmlFile,
//                         html.Replace("site.js", $"site.min.js?_t={DateTimeOffset.Now.ToUnixTimeSeconds()}"));
//                 }
//             }
//
//             logger.LogInformation("处理 js 引用完成");
//         }
//
//         if (!string.IsNullOrEmpty(Configuration["BasePath"]))
//         {
//             app.UsePathBase(Configuration["BasePath"]);
//         }
//
//         app.UseHealthChecks("/healthz");
//         app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });
//         app.UseFileServer();
//         app.UseRouting();
//         app.UseCors("cors");
//         app.UseMiddleware<PublicFacingUrlMiddleware>(Configuration);
//         app.UseIdentityServer();
//         app.UseAuthorization();
//         var inDapr = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DAPR_HTTP_PORT"));
//         if (inDapr)
//         {
//             app.UseCloudEvents();
//         }
//
//         app.UseEndpoints(endpoints =>
//         {
//             if (inDapr)
//             {
//                 endpoints.MapSubscribeHandler();
//             }
//
//             endpoints.MapControllers().RequireCors("cors");
//         });
//     }
//
//     // private string[] GetCorsOrigins()
//     // {
//     //     return Configuration.GetSection("AllowedCorsOrigins").Get<string[]>() ?? Array.Empty<string>();
//     // }
// }
