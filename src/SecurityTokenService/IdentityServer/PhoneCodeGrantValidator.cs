using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;

namespace SecurityTokenService.IdentityServer;

public class PhoneCodeGrantValidator(
    ILogger<PhoneCodeGrantValidator> logger,
    UserManager<User> userManager,
    IOptionsMonitor<IdentityExtensionOptions> identityExtensionOptions)
    : IExtensionGrantValidator
{
    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        //获取登录参数
        var phoneNumber = context.Request.Raw["phone_number"];
        var verificationCode = context.Request.Raw["code"];
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(verificationCode))
            {
                context.Result = new GrantValidationResult
                {
                    IsError = true, Error = "invalid_phone_number_or_code", ErrorDescription = "手机号或者验证码为空"
                };

                return;
            }

            //根据手机号获取用户信息
            var user = await userManager.GetUserByPhoneNumberAsync(phoneNumber,
                identityExtensionOptions.CurrentValue.SoftDeleteColumn);
            if (user == null)
            {
                context.Result = new GrantValidationResult
                {
                    IsError = true, ErrorDescription = "用户不存在", Error = "user_not_exists",
                };

                return;
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                context.Result = new GrantValidationResult
                {
                    IsError = true, Error = "user_is_locked_out", ErrorDescription = "用户被锁定"
                };

                return;
            }

            var isValid = await userManager.VerifyUserTokenAsync(user,
                userManager.Options.Tokens.AuthenticatorTokenProvider, Util.PurposeLogin, verificationCode);

            if (!isValid)
            {
                await userManager.AccessFailedAsync(user);
                context.Result = new GrantValidationResult
                {
                    IsError = true, Error = "invalid_code", ErrorDescription = "验证码不正确"
                };

                return;
            }

            // 授权通过返回
            // 返回角色等？
            context.Result = new GrantValidationResult(user.Id, "phone_code",
                customResponse: new Dictionary<string, object>
                {
                    { "expires_at", DateTimeOffset.Now.ToUnixTimeSeconds() + context.Request.AccessTokenLifetime }
                });
            logger.LogInformation("Grant success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Phone} 验证码 {Code} 登录失败", phoneNumber, verificationCode);
            context.Result = new GrantValidationResult
            {
                IsError = true, Error = "unknown_error", ErrorDescription = ex.Message
            };
        }
    }

    public string GrantType => "phone_code";
}
