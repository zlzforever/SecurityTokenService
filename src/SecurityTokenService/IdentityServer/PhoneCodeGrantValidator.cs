using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecurityTokenService.Identity;

namespace SecurityTokenService.IdentityServer;

public class PhoneCodeGrantValidator : IExtensionGrantValidator
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<PhoneCodeGrantValidator> _logger;
    private readonly IPhoneCodeStore _phoneCodeStore;


    public PhoneCodeGrantValidator(
        ILogger<PhoneCodeGrantValidator> logger, UserManager<User> userManager,
        // IdentityDbContext identityDbContext,
        IPhoneCodeStore phoneCodeStore)
    {
        _logger = logger;
        _userManager = userManager;
        // _identityDbContext = identityDbContext;
        _phoneCodeStore = phoneCodeStore;
    }

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        try
        {
            //获取登录参数
            var phoneNumber = context.Request.Raw["phone_number"];
            var verificationCode = context.Request.Raw["code"];
            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(verificationCode))
            {
                context.Result = new GrantValidationResult
                {
                    IsError = true,
                    Error = "invalid_phone_number_or_code",
                    ErrorDescription = "手机号或者验证码为空"
                };

                return;
            }

            //根据手机号获取用户信息
            var user = await GetUserByPhoneNumberAsync(phoneNumber);
            if (user == null)
            {
                context.Result = new GrantValidationResult
                {
                    IsError = true,
                    ErrorDescription = "用户不存在",
                    Error = "user_not_exists",
                };

                return;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                context.Result = new GrantValidationResult
                {
                    IsError = true,
                    Error = "user_is_locked_out",
                    ErrorDescription = "用户被锁定"
                };

                return;
            }

            var code = await _phoneCodeStore.GetAsync(phoneNumber);
            //获取手机号对应的缓存验证码
            if (string.IsNullOrEmpty(code))
            {
                //如果获取不到缓存验证码，说明手机号不存在，或者验证码过期，但是发送验证码时已经验证过手机号是存在的，所以只能是验证码过期
                context.Result = new GrantValidationResult
                {
                    IsError = true,
                    Error = "code_is_expired",
                    ErrorDescription = "验证码过期"
                };

                return;
            }


            if (verificationCode != code)
            {
                await _userManager.AccessFailedAsync(user);
                context.Result = new GrantValidationResult
                {
                    IsError = true,
                    Error = "invalid_code",
                    ErrorDescription = "验证码不正确"
                };

                return;
            }

            // 授权通过返回
            // 返回角色等？
            context.Result = new GrantValidationResult(user.Id, "custom", customResponse: new Dictionary<string, object>
            {
                { "expires_at", DateTimeOffset.Now.ToUnixTimeSeconds() + context.Request.AccessTokenLifetime }
            });
            _logger.LogInformation("Grant success");
        }
        catch (Exception ex)
        {
            context.Result = new GrantValidationResult
            {
                IsError = true,
                Error = "unknown_error",
                ErrorDescription = ex.Message
            };
        }
    }

    //根据手机号获取用户信息
    private async Task<User> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        return user;
    }

    public string GrantType => "phone_code";
}