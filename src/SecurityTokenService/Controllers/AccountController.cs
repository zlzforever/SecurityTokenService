using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;
using SecurityTokenService.Sms;
using SecurityTokenService.Utils;

namespace SecurityTokenService.Controllers;

[SecurityHeaders]
[AllowAnonymous]
[Route("[controller]")]
public class AccountController(
    IIdentityServerInteractionService interaction,
    IEventService events,
    SignInManager<User> signInManager,
    IOptionsMonitor<SecurityTokenServiceOptions> options,
    IOptionsMonitor<IdentityExtensionOptions> identityExtensionOptions,
    UserManager<User> userManager,
    ILogger<AccountController> logger,
    IHostEnvironment hostEnvironment,
    // IPhoneCodeStore phoneCodeStore,
    IPasswordValidator<User> passwordValidator,
    ISmsSender smsSender,
    IMemoryCache memoryCache)
    : ControllerBase
{
    private readonly SecurityTokenServiceOptions _options = options.CurrentValue;
    private readonly IdentityExtensionOptions _identityExtensionOptions = identityExtensionOptions.CurrentValue;

    /// <summary>
    /// 通过旧密码修改密码
    /// 要提供用户名
    /// </summary>
    /// <returns></returns>
    [HttpPost("ResetPwdByOriginPwd")]
    public async Task<IActionResult> ResetPasswordByOriginPassword(
        [FromBody] Inputs.V1.ResetPasswordByOriginPasswordInput input)
    {
        var modelErrorResult = BuildModelValidApiResult();
        if (modelErrorResult != null)
        {
            return new ObjectResult(modelErrorResult);
        }

        var checkCaptchaResult = CheckCaptcha(input.CaptchaCode);
        if (checkCaptchaResult != null)
        {
            return new ObjectResult(checkCaptchaResult);
        }

        var user = await userManager.FindAsync(input.UserName,
            _identityExtensionOptions.SoftDeleteColumn);

        var availableResult = await CheckUserAvailableAsync(user);
        if (availableResult != null)
        {
            return new ObjectResult(availableResult);
        }

        var checkPasswordResult = await userManager.CheckPasswordAsync(user, input.OldPassword);
        if (checkPasswordResult)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            // 若设置了密码策略此方法内部会校验
            var result = await userManager.ResetPasswordAsync(user, token, input.ConfirmNewPassword);
            if (result.Succeeded)
            {
                return new ObjectResult(new ApiResult { Message = "修改成功" });
            }

            var msg = string.Join(Environment.NewLine, result.Errors.Select(x => x.Description));
            logger.LogError("用户 {User} 重置密码失败: {Info} ", input.UserName, msg);

            return new ObjectResult(
                new ApiResult { Code = Errors.ChangePasswordFailed, Success = false, Message = "重置密码失败" });
        }

        return new ObjectResult(new ApiResult
        {
            Code = Errors.IdentityInvalidCredentials, Success = false, Message = "用户名或密码不正确"
        });
    }

    /// <summary>
    /// 通过手机号修改密码
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("ResetPwd")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] Inputs.V1.ResetPasswordByPhoneNumberInput input)
    {
        var modelErrorResult = BuildModelValidApiResult();
        if (modelErrorResult != null)
        {
            return new ObjectResult(modelErrorResult);
        }

        var user = await userManager.FindAsync(input.PhoneNumber, _identityExtensionOptions.SoftDeleteColumn);

        var availableResult = await CheckUserAvailableAsync(user);
        if (availableResult != null)
        {
            return new ObjectResult(availableResult);
        }

        var result = await userManager.ResetPasswordAsync(user, input.VerifyCode, input.ConfirmNewPassword);

        if (!result.Succeeded)
        {
            var msg = string.Join(Environment.NewLine, result.Errors.Select(x => x.Description));
            // TODO: 手机号脱敏存日志
            logger.LogError("用户 {User} 重置密码失败: {Info} ", input.PhoneNumber, msg);
            return new ObjectResult(
                new ApiResult { Code = Errors.ChangePasswordFailed, Success = false, Message = "重置密码失败" });
        }

        return new ObjectResult(new ApiResult { Message = "修改成功" });
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] Inputs.V1.LoginInput model)
    {
        var modelErrorResult = BuildModelValidApiResult();
        if (modelErrorResult != null)
        {
            return new ObjectResult(modelErrorResult);
        }

        if (_options.ForcePasswordSecurityPolicy)
        {
            var passwordValidateResult =
                await passwordValidator.ValidateAsync(userManager, new User(""), model.Password);
            if (!passwordValidateResult.Succeeded)
            {
                return new ObjectResult(new ApiResult
                {
                    Code = Errors.PasswordValidateFailed, Success = false, Message = "密码不符合安全要求， 请先修改密码"
                });
            }
        }

        var context = await interaction.GetAuthorizationContextAsync(model.ReturnUrl);
        // the user clicked the "cancel" button
        if (model.Button != "login")
        {
            if (context != null)
            {
                // if the user cancels, send a result back into IdentityServer as if they 
                // denied the consent (even if this client does not require consent).
                // this will send back an access denied OIDC error response to the client.
                await interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.

                    return new ObjectResult(new ApiResult
                    {
                        Code = Errors.IdentityNativeClientIsNotSupported,
                        Success = false,
                        Message = "不支持 NativeClient"
                    });
                }

                return new ObjectResult(new RedirectResult(model.ReturnUrl));
            }

            // since we don't have a valid context, then we just go back to the home page
            return new ObjectResult(new RedirectResult("/"));
        }

        var checkCaptchaResult = CheckCaptcha(model.CaptchaCode);
        if (checkCaptchaResult != null)
        {
            return new ObjectResult(checkCaptchaResult);
        }

        var user = await userManager.FindAsync(model.Username, _identityExtensionOptions.SoftDeleteColumn);

        if (user == null)
        {
            await events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials",
                clientId: context?.Client.ClientId));
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityInvalidCredentials, Success = false, Message = "用户名或密码错误"
            });
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password,
            model.RememberLogin, true);
        if (result.Succeeded)
        {
            await events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName,
                clientId: context?.Client.ClientId));

            if (context != null)
            {
                // if (await _clientStore.IsPkceClientAsync(context.Client.ClientId))
                // {
                //     // if the client is PKCE then we assume it's native, so this change in how to
                //     // return the response is for better UX for the end user.
                //     // TODO: 意义是说若是 PKCE 客户端，应该返回页面内容，而不是让终端用户自己跳转
                //     // 但这样， 不好定义纯 HTML 的内容
                //     return new ObjectResult(new RedirectResult(model.ReturnUrl));
                //     // return new ObjectResult(new
                //     // {
                //     //     Code = 302,
                //     //     Location =
                //     //         $"/redirect.html?redirectUrl={HttpUtility.UrlEncode(model.ReturnUrl)}&_t={DateTimeOffset.Now.ToUnixTimeSeconds()}"
                //     // });
                // }
                // else
                // {
                //     // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                //     return new ObjectResult(new RedirectResult(model.ReturnUrl));
                // }
                // return new ObjectResult(new RedirectResult(model.ReturnUrl));
            }

            return new ObjectResult(new RedirectResult(model.ReturnUrl));
        }

        // TODO: 2 次认证需要支持
        if (result.RequiresTwoFactor)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityTwoFactorIsNotSupported, Success = false, Message = "多因素认证不支持"
            });
        }

        if (result.IsNotAllowed)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityUserIsNotAllowed, Success = false, Message = "用户被禁用"
            });
        }

        if (result.IsLockedOut)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityUserIsLockedOut, Success = false, Message = "用户被锁定"
            });
        }

        await events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials",
            clientId: context?.Client.ClientId));
        return new ObjectResult(
            new ApiResult { Code = Errors.IdentityInvalidCredentials, Success = false, Message = "用户名或密码错误" });

        // something went wrong, show form with error
        // var vm = await BuildLoginViewModelAsync(model);
        // return View(vm);
    }

    /// <summary>
    /// 通过验证码登录
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("LoginByCode")]
    public async Task<IActionResult> LoginByCode([FromBody] Inputs.V1.LoginBySmsInput model)
    {
        var modelErrorResult = BuildModelValidApiResult();
        if (modelErrorResult != null)
        {
            return new ObjectResult(modelErrorResult);
        }

        var context = await interaction.GetAuthorizationContextAsync(model.ReturnUrl);
        // the user clicked the "cancel" button
        if (model.Button != "login")
        {
            if (context != null)
            {
                // if the user cancels, send a result back into IdentityServer as if they 
                // denied the consent (even if this client does not require consent).
                // this will send back an access denied OIDC error response to the client.
                await interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.

                    return new ObjectResult(new ApiResult
                    {
                        Code = Errors.IdentityNativeClientIsNotSupported,
                        Success = false,
                        Message = "不支持 NativeClient"
                    });
                }

                return new ObjectResult(new RedirectResult(model.ReturnUrl));
            }

            // since we don't have a valid context, then we just go back to the home page
            return new ObjectResult(new RedirectResult("/"));
        }

        var user = await userManager.FindAsync(model.PhoneNumber, _identityExtensionOptions.SoftDeleteColumn);

        if (user == null)
        {
            await events.RaiseAsync(new UserLoginFailureEvent(model.PhoneNumber, "invalid credentials",
                clientId: context?.Client.ClientId));
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityInvalidCredentials, Success = false, Message = "用户不存在"
            });
        }

        var isValid = await userManager.VerifyUserTokenAsync(user, Util.PhoneNumberTokenProvider, Util.PurposeLogin,
            model.VerifyCode);
        if (!isValid)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.VerifyCodeIsInCorrect, Success = false, Message = "手机验证码不正确"
            });
        }

        await signInManager.SignInAsync(user, true);
        // 清除 TOKEN 有效
        await userManager.UpdateSecurityStampAsync(user);

        await events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName,
            clientId: context?.Client.ClientId));

        if (context == null)
        {
            return new ObjectResult(
                new RedirectResult(string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl));
        }

        return new ObjectResult(new RedirectResult(model.ReturnUrl));
    }

    /// <summary>
    /// 发送短信验证码
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("SendCode")]
    public async Task<ApiResult> SendCode([FromBody] Inputs.V1.SendCode input)
    {
        var modelErrorResult = BuildModelValidApiResult();
        if (modelErrorResult != null)
        {
            return modelErrorResult;
        }

        var key = $"SMS:PHONE:LIMIT:{input.PhoneNumber}";
        var timestamp = memoryCache.Get<long?>(key);
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (timestamp != null && now - timestamp < 60)
        {
            // TODO: 手机号要脱敏
            logger.LogWarning("{Phone} 验证码发送过于频繁", input.PhoneNumber);
            return new ApiResult { Code = 429, Message = "验证码发送过于频繁，请稍后再试", Success = false };
        }

        var checkCaptchaResult = CheckCaptcha(input.CaptchaCode);
        if (checkCaptchaResult != null)
        {
            return checkCaptchaResult;
        }

        if ("Login".Equals(input.Scenario, StringComparison.OrdinalIgnoreCase))
        {
            var user = await userManager.FindAsync(input.PhoneNumber, _identityExtensionOptions.SoftDeleteColumn);
            var availableResult = await CheckUserAvailableAsync(user);
            if (availableResult != null)
            {
                switch (availableResult.Code)
                {
                    case Errors.IdentityUserIsNotExist:
                        // 用户不存在只返回成功不发送验证码
                        logger.LogWarning($"{input.PhoneNumber} 手机用户:{availableResult.Message}");
                        return new ApiResult { Success = true, Message = "发送成功" };
                    default:
                        return availableResult;
                }
            }

            return await SendCodeAsync(key, user, input, Util.PurposeLogin);
        }

        if ("ResetPassword".Equals(input.Scenario, StringComparison.OrdinalIgnoreCase))
        {
            var user = await userManager.FindAsync(input.PhoneNumber, _identityExtensionOptions.SoftDeleteColumn);
            var availableResult = await CheckUserAvailableAsync(user);
            if (availableResult != null)
            {
                return availableResult;
            }

            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            return await SendCodeAsync(key, input, code);
        }

        if ("Register".Equals(input.Scenario, StringComparison.OrdinalIgnoreCase))
        {
            // TODO
            return await SendCodeAsync(key, null, input, Util.PurposeRegister);
        }

        return new ApiResult { Code = 404, Message = "参数错误", Success = false };
    }

    [HttpGet("Logout")]
    public async Task<IActionResult> Logout(string logoutId)
    {
        var vm = await BuildLogoutOutputAsync(logoutId);

        if (vm.ShowLogoutPrompt == false)
        {
            // if the request for logout was properly authenticated from IdentityServer, then
            // we don't need to show the prompt and can just log the user out directly.
            return await Logout(new Inputs.V1.LogoutInput { LogoutId = logoutId });
        }

        return Redirect($"~/logout.html?logoutId={vm.LogoutId}");
    }

    [HttpPost("Logout")]
    public async Task<IActionResult> Logout([FromForm] Inputs.V1.LogoutInput model)
    {
        // build a model so the logged out page knows what to display
        var vm = await BuildLoggedOutOutputAsync(model.LogoutId);

        if (User.Identity?.IsAuthenticated == true)
        {
            // delete local authentication cookie
            await signInManager.SignOutAsync();

            // raise the logout event
            await events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
        }

        // check if we need to trigger sign-out at an upstream identity provider
        if (vm.TriggerExternalSignout)
        {
            // build a return URL so the upstream provider will redirect back
            // to us after the user has logged out. this allows us to then
            // complete our single sign-out processing.
            var url = Url.Action("Logout", new { logoutId = vm.LogoutId });

            // this triggers a redirect to the external provider for sign-out
            return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
        }

        var postLogoutRedirect = HttpUtility.UrlEncode(vm.PostLogoutRedirectUri);
        var signOutIframe = HttpUtility.UrlEncode(vm.SignOutIframeUrl);
        var query =
            $"postLogoutRedirectUri={postLogoutRedirect}&clientName={HttpUtility.UrlEncode(vm.ClientName)}&signOutIframeUrl={signOutIframe}&automaticRedirectAfterSignOut={(vm.AutomaticRedirectAfterSignOut ? "true" : "false")}";

        return Redirect(
            $"~/loggedout.html?" + query);
    }

    private async Task<Outputs.V1.LoggedOutOutput> BuildLoggedOutOutputAsync(string logoutId)
    {
        var logout = await interaction.GetLogoutContextAsync(logoutId);

        var vm = new Outputs.V1.LoggedOutOutput
        {
            AutomaticRedirectAfterSignOut = _options.AutomaticRedirectAfterSignOut,
            PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
            ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout.ClientName,
            SignOutIframeUrl = logout?.SignOutIFrameUrl,
            LogoutId = logoutId
        };

        if (User.Identity?.IsAuthenticated != true)
        {
            return vm;
        }

        var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
        if (idp is null or IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
        {
            return vm;
        }

        var providerSupportsSignOut = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
        if (!providerSupportsSignOut)
        {
            return vm;
        }

        vm.LogoutId ??= await interaction.CreateLogoutContextAsync();
        vm.ExternalAuthenticationScheme = idp;

        return vm;
    }

    private async Task<Outputs.V1.LogoutOutput> BuildLogoutOutputAsync(string logoutId)
    {
        var vm = new Outputs.V1.LogoutOutput { LogoutId = logoutId, ShowLogoutPrompt = _options.ShowLogoutPrompt };

        if (User.Identity?.IsAuthenticated != true)
        {
            // if the user is not authenticated, then just show logged out page
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        var context = await interaction.GetLogoutContextAsync(logoutId);
        if (context?.ShowSignoutPrompt != false)
        {
            return vm;
        }

        // it's safe to automatically sign-out
        vm.ShowLogoutPrompt = false;
        return vm;

        // show the logout prompt. this prevents attacks where the user
        // is automatically signed out by another malicious web page.
    }

    private ApiResult BuildModelValidApiResult()
    {
        if (!ModelState.IsValid)
        {
            var errors = new List<ModelError>();
            foreach (var stateEntry in ModelState)
            {
                if (stateEntry.Value.ValidationState != ModelValidationState.Invalid)
                {
                    continue;
                }

                foreach (var error in stateEntry.Value.Errors)
                {
                    errors.Add(error);
                }
            }

            var msg = string.Join("\n", errors.Select(x => x.ErrorMessage));
            return new ApiResult { Code = 400, Success = false, Message = msg };
        }

        return null;
    }

    // private ObjectResult BuildModelValidResult()
    // {
    //     var apiErrorResult = BuildModelValidApiResult();
    //     return apiErrorResult == null ? null : new ObjectResult(apiErrorResult);
    // }

    private async Task<ApiResult> SendCodeAsync(string key, User user, Inputs.V1.SendCode input, string purpose)
    {
        var code = await userManager.GenerateUserTokenAsync(user, Util.PhoneNumberTokenProvider, purpose);
        return await SendCodeAsync(key, input, code);
    }

    private async Task<ApiResult> SendCodeAsync(string key, Inputs.V1.SendCode input, string code)
    {
        var countryCode = string.IsNullOrWhiteSpace(input.CountryCode) ? "+86" : input.CountryCode;
        var phoneNumber = $"{countryCode} {input.PhoneNumber}";

        try
        {
            if (hostEnvironment.IsDevelopment())
            {
                logger.LogWarning("[SMS] Send to {PhoneNumber} {Code} success", input.PhoneNumber, code);
            }
            else
            {
                await smsSender.SendAsync(phoneNumber, code);
            }

            memoryCache.Set(key, DateTimeOffset.Now.ToUnixTimeSeconds(), TimeSpan.FromSeconds(60));
            return new ApiResult { Success = true, Message = "发送成功" };
        }
        catch (FriendlyException fe)
        {
            return new ApiResult { Message = fe.Message, Success = false, Code = Errors.SendSmsFailed };
        }
    }

    private ApiResult CheckCaptcha(string captchaCode)
    {
        var captchaId = Request.Cookies["CaptchaId"];
        if (string.IsNullOrEmpty(captchaId))
        {
            return new ApiResult { Code = Errors.VerifyCodeIsExpired, Success = false, Message = "验证码已过期， 请刷新" };
        }

        var cacheKey = string.Format(Util.CaptchaTtlKey, captchaId);
        var realCaptchaCode = memoryCache.Get<string>(cacheKey);
        // 步骤3：校验验证码
        if (string.IsNullOrEmpty(realCaptchaCode))
        {
            return new ApiResult { Code = Errors.VerifyCodeIsExpired, Success = false, Message = "验证码已过期， 请刷新" };
        }

        if (!string.Equals(realCaptchaCode, captchaCode, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError("图形验证码校验失败, {CaptchaId} {RealCaptchaCode} {Actual}", captchaId, realCaptchaCode,
                captchaCode);
            return new ApiResult { Code = Errors.VerifyCodeIsInCorrect, Success = false, Message = "验证码错误" };
        }

        // 步骤4：验证码验证通过后，删除缓存（防止重复使用）
        memoryCache.Remove(cacheKey);
        return null;
    }

    private async Task<ApiResult> CheckUserAvailableAsync(User user)
    {
        if (user == null)
        {
            return new ApiResult { Code = Errors.IdentityUserIsNotExist, Success = false, Message = "用户不存在" };
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return new ApiResult { Code = Errors.IdentityUserIsLockedOut, Success = false, Message = "用户被锁定" };
        }

        return null;
    }
}
