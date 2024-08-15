using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;
using SecurityTokenService.Sms;
using SecurityTokenService.Stores;

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
    IPhoneCodeStore phoneCodeStore,
    IPasswordValidator<User> passwordValidator,
    ISmsSender smsSender)
    : ControllerBase
{
    private readonly SecurityTokenServiceOptions _options = options.CurrentValue;
    private readonly IdentityExtensionOptions _identityExtensionOptions = identityExtensionOptions.CurrentValue;

    /// <summary>
    /// 通过旧密码修改密码
    /// 要提供用户名
    /// </summary>
    /// <returns></returns>
    [HttpPost("ResetPassword2")]
    public async Task<IActionResult> ResetPasswordByOldPasswordAsync(
        Inputs.V1.ResetPasswordByOldPasswordInput input)
    {
        var modelErrorResult = BuildModelValidResult();
        if (modelErrorResult != null)
        {
            return modelErrorResult;
        }

        var user = await userManager.FindAsync(input.UserName,
            _identityExtensionOptions.SoftDeleteColumn);

        if (user == null)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityUserIsNotExist, Success = false, Message = "用户不存在"
            });
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityUserIsLockedOut, Success = false, Message = "用户被锁定"
            });
        }

        var passwordValidateResult =
            await passwordValidator.ValidateAsync(userManager, user, input.NewPassword);
        if (!passwordValidateResult.Succeeded)
        {
            var msg = string.Join(Environment.NewLine, passwordValidateResult.Errors.Select(x => x.Description));
            return new ObjectResult(new ApiResult
            {
                Code = Errors.PasswordValidateFailed, Success = false, Message = msg
            });
        }

        var checkPasswordResult = await userManager.CheckPasswordAsync(user, input.OldPassword);
        if (checkPasswordResult)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, input.ConfirmNewPassword);
            if (result.Succeeded)
            {
                return new ObjectResult(new ApiResult { Message = "修改成功" });
            }

            var msg = string.Join(Environment.NewLine, result.Errors.Select(x => x.Description));
            return new ObjectResult(new ApiResult
            {
                Code = Errors.ChangePasswordFailed, Success = false, Message = msg
            });
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
    [HttpPost("ResetPassword")]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromBody] Inputs.V1.ResetPasswordByPhoneNumberInput input)
    {
        var modelErrorResult = BuildModelValidResult();
        if (modelErrorResult != null)
        {
            return modelErrorResult;
        }

        var user = await userManager.FindAsync(input.PhoneNumber,
            _identityExtensionOptions.SoftDeleteColumn);

        if (user == null)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityUserIsNotExist, Success = false, Message = "用户不存在"
            });
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityUserIsLockedOut, Success = false, Message = "用户被锁定"
            });
        }

        var passwordValidateResult =
            await passwordValidator.ValidateAsync(userManager, user, input.NewPassword);
        if (!passwordValidateResult.Succeeded)
        {
            var msg = string.Join(Environment.NewLine, passwordValidateResult.Errors.Select(x => x.Description));
            return new ObjectResult(new ApiResult
            {
                Code = Errors.PasswordValidateFailed, Success = false, Message = msg
            });
        }

        var code = await phoneCodeStore.GetAsync(input.PhoneNumber);
        //获取手机号对应的缓存验证码
        if (string.IsNullOrEmpty(code) || input.VerifyCode != code)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.VerifyCodeIsInCorrect, Success = false, Message = "验证码不正确"
            });
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, input.ConfirmNewPassword);

        if (!result.Succeeded)
        {
            var msg = string.Join(Environment.NewLine, result.Errors.Select(x => x.Description));
            return new ObjectResult(new ApiResult
            {
                Code = Errors.ChangePasswordFailed, Success = false, Message = msg
            });
        }

        await phoneCodeStore.UpdateAsync(input.PhoneNumber, "");
        return new ObjectResult(new ApiResult { Message = "修改成功" });
    }

    [HttpPost("Login")]
    public async Task<IActionResult> LoginAsync(Inputs.V1.LoginInput model)
    {
        var modelErrorResult = BuildModelValidResult();
        if (modelErrorResult != null)
        {
            return modelErrorResult;
        }

        var passwordValidateResult = await passwordValidator.ValidateAsync(userManager, null, model.Password);
        if (!passwordValidateResult.Succeeded)
        {
            var message = string.Join(Environment.NewLine,
                passwordValidateResult.Errors.Select(x => x.Description));
            return new ObjectResult(new ApiResult
            {
                Code = Errors.PasswordValidateFailed, Success = false, Message = $"{message}\n请先修改密码后再登录"
            });
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

        var user = await userManager.FindAsync(model.Username, _identityExtensionOptions.SoftDeleteColumn);

        if (user == null)
        {
            await events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials",
                clientId: context?.Client.ClientId));
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityInvalidCredentials, Success = false, Message = "用户不存在"
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
                return new ObjectResult(new RedirectResult(model.ReturnUrl));
            }

            return new ObjectResult(new RedirectResult(model.ReturnUrl));
        }

        // TODO: 2 次认证需要支持
        if (result.RequiresTwoFactor)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityTwoFactorIsNotSupported, Success = false, Message = ""
            });
        }

        if (result.IsNotAllowed)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityUserIsNotAllowed, Success = false, Message = "禁止登录"
            });
        }

        if (result.IsLockedOut)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.IdentityUserIsLockedOut, Success = false, Message = "帐号被锁定"
            });
        }

        await events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials",
            clientId: context?.Client.ClientId));
        return new ObjectResult(
            new ApiResult { Code = Errors.IdentityInvalidCredentials, Success = false, Message = "登录失败" });

        // something went wrong, show form with error
        // var vm = await BuildLoginViewModelAsync(model);
        // return View(vm);
    }

    /// <summary>
    /// 通过短信登录
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("LoginBySms")]
    public async Task<IActionResult> LoginBySmsAsync(Inputs.V1.LoginBySmsInput model)
    {
        if (!ModelState.IsValid)
        {
            var messageBuilder = new StringBuilder();
            foreach (var stateEntry in ModelState)
            {
                if (stateEntry.Value.ValidationState != ModelValidationState.Invalid)
                {
                    continue;
                }

                foreach (var error in stateEntry.Value.Errors)
                {
                    messageBuilder.AppendLine(error.ErrorMessage);
                }
            }

            return new ObjectResult(new ApiResult
            {
                Code = 400, Success = false, Message = messageBuilder.ToString()
            });
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

        var code = await phoneCodeStore.GetAsync(model.PhoneNumber);
        //获取手机号对应的缓存验证码
        if (string.IsNullOrEmpty(code) || model.VerifyCode != code)
        {
            return new ObjectResult(new ApiResult
            {
                Code = Errors.VerifyCodeIsInCorrect, Success = false, Message = "验证码不正确"
            });
        }

        await signInManager.SignInAsync(user, true);

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
    [HttpPost("SendSmsCode")]
    [HttpPost("sms")]
    public async Task<ApiResult> SendSmsCodeAsync([FromBody] Inputs.V1.SendSmsCode input)
    {
        var modelErrorResult = BuildModelValidApiResult();
        if (modelErrorResult != null)
        {
            return modelErrorResult;
        }

        // 不存在也应该发短信， 因为可以是通过短信注册的
        // var user = await _userManager.FindAsync(input.PhoneNumber, _identityExtensionOptions.SoftDeleteColumn);
        // if (user != null)
        // {
        //     if (await _userManager.IsLockedOutAsync(user))
        //     {
        //         return new ApiResult { Code = Errors.IdentityUserIsLockedOut, Success = false, Message = "帐号被锁定" };
        //     }
        // }

        var code = RandomNumberGenerator.GetInt32(1111, 9999).ToString();

        var countryCode = string.IsNullOrWhiteSpace(input.CountryCode) ? "+86" : input.CountryCode;
        var phoneNumber = $"{countryCode} {input.PhoneNumber}";
        await phoneCodeStore.UpdateAsync(input.PhoneNumber, code);

        if (hostEnvironment.IsDevelopment())
        {
            logger.LogInformation($"Send sms code to {input.PhoneNumber}: {code}");
            return new ApiResult { Success = true };
        }

        try
        {
            await smsSender.SendAsync(phoneNumber, code);
            return new ApiResult { Success = true, Message = "发送成功" };
        }
        catch (FriendlyException fe)
        {
            return new ApiResult { Message = fe.Message, Success = false, Code = Errors.SendSmsFailed };
        }
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
            var messageBuilder = new StringBuilder();
            foreach (var stateEntry in ModelState)
            {
                if (stateEntry.Value.ValidationState != ModelValidationState.Invalid)
                {
                    continue;
                }

                foreach (var error in stateEntry.Value.Errors)
                {
                    messageBuilder.AppendLine(error.ErrorMessage);
                }
            }

            return new ApiResult { Code = 400, Success = false, Message = messageBuilder.ToString() };
        }
        else
        {
            return null;
        }
    }

    private ObjectResult BuildModelValidResult()
    {
        var apiErrorResult = BuildModelValidApiResult();
        return apiErrorResult == null ? null : new ObjectResult(apiErrorResult);
    }
}
