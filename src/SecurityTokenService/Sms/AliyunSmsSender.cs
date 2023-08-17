using System;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityTokenService.Options;

namespace SecurityTokenService.Sms;

public class AliyunSmsSender : ISmsSender
{
    private readonly AliyunOptions _aliyunOptions;
    private readonly ILogger<AliyunSmsSender> _logger;

    public AliyunSmsSender(IOptionsMonitor<AliyunOptions> aliyunOptions, ILogger<AliyunSmsSender> logger)
    {
        _logger = logger;
        _aliyunOptions = aliyunOptions.CurrentValue;
    }

    public async Task SendAsync(string number, string code)
    {
        var smsClient = CreateClient();
        var pieces = number.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length != 2)
        {
            throw new FriendlyException("电话号码缺少国家码");
        }

        var countryCode = pieces[0];
        var template = _aliyunOptions.Sms.Templates.GetOrDefault(countryCode);
        if (string.IsNullOrEmpty(template))
        {
            _logger.LogError($"CountryCode {countryCode} no sms template");
            throw new FriendlyException("不支持的国家");
        }

        var request =
            new AlibabaCloud.SDK.Dysmsapi20170525.Models.SendSmsRequest
            {
                PhoneNumbers = number,
                SignName = _aliyunOptions.Sms.SignName,
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

            _logger.LogError($"{number} {response.Body.Message}");
            throw new FriendlyException("发送验证码失败");
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
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
            AccessKeyId = _aliyunOptions.AccessKey,
            // 您的AccessKey Secret
            AccessKeySecret = _aliyunOptions.Secret,
            Endpoint = _aliyunOptions.Endpoint,
        };
        // 访问的域名
        return new AlibabaCloud.SDK.Dysmsapi20170525.Client(config);
    }
}
