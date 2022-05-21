using System;
using System.Threading.Tasks;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
                    Error = "电话号码或验证码为空",
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
                    Error = "手机号无效",
                };

                return;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                context.Result = new GrantValidationResult
                {
                    IsError = true,
                    Error = "账户已锁定",
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
                    Error = "验证码过期",
                };

                return;
            }


            if (verificationCode != code)
            {
                await _userManager.AccessFailedAsync(user);
                context.Result = new GrantValidationResult
                {
                    IsError = true,
                    Error = "验证码错误",
                };

                return;
            }

            // 授权通过返回
            // 返回角色等？
            context.Result = new GrantValidationResult(user.Id, "custom");
            _logger.LogInformation("Grant success");
        }
        catch (Exception ex)
        {
            context.Result = new GrantValidationResult
            {
                IsError = true,
                Error = ex.Message
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