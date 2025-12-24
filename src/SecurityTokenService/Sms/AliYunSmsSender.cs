using System;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityTokenService.Options;

namespace SecurityTokenService.Sms;

public class AliYunSmsSender(
    IOptionsMonitor<AliYunOptions> aliyunOptions,
    ILogger<AliYunSmsSender> logger)
    : ISmsSender
{
    private readonly AliYunOptions _aliYunOptions = aliyunOptions.CurrentValue;

    public async Task SendAsync(string number, string code)
    {
        var smsClient = CreateClient();
        var pieces = number.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length != 2)
        {
            throw new FriendlyException("电话号码缺少国家码");
        }

        var countryCode = pieces[0];
        var template = _aliYunOptions.Sms.Templates.GetOrDefault(countryCode);
        if (string.IsNullOrEmpty(template))
        {
            logger.LogError($"CountryCode {countryCode} no sms template");
            throw new FriendlyException("不支持的国家");
        }

        var phone = pieces[1];

        var request =
            new AlibabaCloud.SDK.Dysmsapi20170525.Models.SendSmsRequest
            {
                PhoneNumbers = phone,
                SignName = _aliYunOptions.Sms.SignName,
                TemplateCode = template,
                TemplateParam = JsonSerializer.Serialize(new { code })
            };

        try
        {
            var response = await smsClient.SendSmsAsync(request);
            if (response.Body.Code == "OK")
            {
                return;
            }

            logger.LogError($"{number} {response.Body.Message}");
            throw new FriendlyException("发送验证码失败");
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            throw new FriendlyException("发送验证码失败");
        }
    }

    /**
         * 使用AK SK初始化账号Client
         * @param accessKeyId
         * @param accessKeySecret
         * @return Client
         * @throws Exception
         */
    private AlibabaCloud.SDK.Dysmsapi20170525.Client CreateClient()
    {
        var config = new AlibabaCloud.OpenApiClient.Models.Config
        {
            // 您的AccessKey ID
            AccessKeyId = _aliYunOptions.AccessKey,
            // 您的AccessKey Secret
            AccessKeySecret = _aliYunOptions.Secret,
            Endpoint = _aliYunOptions.Endpoint,
        };
        // 访问的域名
        return new AlibabaCloud.SDK.Dysmsapi20170525.Client(config);
    }
}
