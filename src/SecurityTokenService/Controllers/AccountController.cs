using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using AutoMapper.Internal;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityTokenService.Extensions;
using SecurityTokenService.Identity;
using SecurityTokenService.IdentityServer;
using SecurityTokenService.IdentityServer.Stores;

namespace SecurityTokenService.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly SignInManager<User> _signInManager;
        private readonly SecurityTokenServiceOptions _options;
        private readonly IClientStore _clientStore;
        private readonly IdentityExtensionOptions _identityExtensionOptions;
        private readonly UserManager<User> _userManager;
        private readonly AliyunSMSOptions _aliyunSmsOptions;
        private readonly AliyunOptions _aliyunOptions;
        private readonly ILogger<AccountController> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IPhoneCodeStore _phoneCodeStore;
        private readonly IPasswordValidator<User> _passwordValidator;
        private readonly IPasswordSecurityInfoStore _passwordSecurityInfoStore;

        public AccountController(IIdentityServerInteractionService interaction, IEventService events,
            SignInManager<User> signInManager, IOptionsMonitor<SecurityTokenServiceOptions> options,
            IOptionsMonitor<IdentityExtensionOptions> identityExtensionOptions,
            IClientStore clientStore, UserManager<User> userManager,
            IOptionsMonitor<AliyunSMSOptions> aliyunSmsOptions, ILogger<AccountController> logger,
            IHostEnvironment hostEnvironment, IPhoneCodeStore phoneCodeStore,
            IOptionsMonitor<AliyunOptions> aliyunOptions, IPasswordValidator<User> passwordValidator,
            IPasswordSecurityInfoStore passwordSecurityInfoStore)
        {
            _interaction = interaction;
            _events = events;

            _signInManager = signInManager;
            _clientStore = clientStore;
            _userManager = userManager;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _phoneCodeStore = phoneCodeStore;
            _passwordValidator = passwordValidator;
            _passwordSecurityInfoStore = passwordSecurityInfoStore;
            _aliyunOptions = aliyunOptions.CurrentValue;
            _aliyunSmsOptions = aliyunSmsOptions.CurrentValue;
            _options = options.CurrentValue;
            _identityExtensionOptions = identityExtensionOptions.CurrentValue;
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] Inputs.V1.ResetPasswordInput resetPassword)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == resetPassword.PhoneNumber);

            if (user == null)
            {
                return new ObjectResult(new ApiResult { Code = Errors.IdentityUserIsNotExist, Message = "用户不存在" });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return new ObjectResult(new ApiResult { Code = Errors.IdentityUserIsLockedOut, Message = "用户被锁定" });
            }

            var passwordValidateResult =
                await _passwordValidator.ValidateAsync(_userManager, user, resetPassword.NewPassword);
            if (!passwordValidateResult.Succeeded)
            {
                var msg = string.Join(Environment.NewLine, passwordValidateResult.Errors.Select(x => x.Description));
                return new ObjectResult(new ApiResult { Code = Errors.PasswordValidateFailed, Message = msg });
            }

            var code = await _phoneCodeStore.GetAsync(resetPassword.PhoneNumber);
            //获取手机号对应的缓存验证码
            if (string.IsNullOrEmpty(code) || resetPassword.VerifyCode != code)
            {
                return new ObjectResult(new ApiResult { Code = Errors.VerifyCodeIsInCorrect, Message = "验证码不正确" });
            }

            if (_identityExtensionOptions.StorePasswordSecurity)
            {
                var passwordLength = resetPassword.NewPassword.Length;
                var passwordContainsDigit = resetPassword.NewPassword.Any(char.IsDigit);
                var passwordContainsLowercase = resetPassword.NewPassword.Any(char.IsLower);
                var passwordContainsUppercase = resetPassword.NewPassword.Any(char.IsUpper);
                var passwordContainsNonAlphanumeric = !resetPassword.NewPassword.All(char.IsLetterOrDigit);

                await _passwordSecurityInfoStore.UpdateAsync(user.Id, passwordLength, passwordContainsDigit,
                    passwordContainsLowercase, passwordContainsUppercase, passwordContainsNonAlphanumeric);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, resetPassword.ConfirmNewPassword);

            if (!result.Succeeded)
            {
                var msg = string.Join(Environment.NewLine, result.Errors.Select(x => x.Description));
                return new ObjectResult(new ApiResult { Code = Errors.ChangePasswordFailed, Message = msg });
            }
            
            await _phoneCodeStore.UpdateAsync(resetPassword.PhoneNumber, "");

            return new ObjectResult(new ApiResult { Code = 200, Message = "修改成功" });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(Inputs.V1.LoginInput model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            // the user clicked the "cancel" button
            if (model.Button != "login")
            {
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.

                        return new ObjectResult(new ApiResult
                        {
                            Code = Errors.IdentityNativeClientIsNotSupported, Message = "不支持 NativeClient"
                        });
                    }

                    return new ObjectResult(new { Code = 302, Location = model.ReturnUrl });
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return new ObjectResult(new { Code = 302, Location = "/" });
                }
            }

            if (!ModelState.IsValid)
            {
                return new ObjectResult(new
                {
                    Code = 302, Location = "/error.html?errorId=" + Errors.IdentityLoginFailed
                });
            }

            // var sql = string.IsNullOrEmpty(identityExtensionOptions.SoftDeleteColumn)
            //     ? $"SELECT * FROM {securityTokenServiceDbContext.Users.EntityType.GetTableName()} WHERE {name} = {{0}} OR {email} = {{0}} OR {phone} = {{0}} LIMIT 1"
            //     : $"SELECT * FROM {securityTokenServiceDbContext.Users.EntityType.GetTableName()} WHERE ({name} = {{0}} OR {email} = {{0}} OR {phone} = {{0}}) AND {identityExtensionOptions.SoftDeleteColumn} != true LIMIT 1";
            User user;

            if (string.IsNullOrWhiteSpace(_identityExtensionOptions.SoftDeleteColumn))
            {
                user = await _userManager.Users.FirstOrDefaultAsync(x =>
                    x.UserName == model.Username || x.Email == model.Username ||
                    x.PhoneNumber == model.Username);
            }
            else
            {
                user = await _userManager.Users
                    .FirstOrDefaultAsync(x =>
                        EF.Property<bool>(x, _identityExtensionOptions.SoftDeleteColumn) == false &&
                        (x.UserName == model.Username || x.Email == model.Username ||
                         x.PhoneNumber == model.Username));
            }

            if (user == null)
            {
                await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials",
                    clientId: context?.Client.ClientId));
                return new ObjectResult(new ApiResult
                {
                    Code = Errors.IdentityInvalidCredentials, Message = "用户名或密码不正确"
                });
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password,
                model.RememberLogin, false);
            if (result.Succeeded)
            {
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName,
                    clientId: context?.Client.ClientId));

                if (context != null)
                {
                    if (await _clientStore.IsPkceClientAsync(context.Client.ClientId))
                    {
                        // if the client is PKCE then we assume it's native, so this change in how to
                        // return the response is for better UX for the end user.
                        return new ObjectResult(new
                        {
                            Code = 302,
                            Location =
                                $"/redirect.html?redirectUrl={HttpUtility.UrlEncode(model.ReturnUrl)}&_t={DateTimeOffset.Now.ToUnixTimeSeconds()}"
                        });
                    }
                    else
                    {
                        // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                        return new ObjectResult(new { Code = 302, Location = model.ReturnUrl });
                    }
                }

                // request for a local page
                if (Url.IsLocalUrl(model.ReturnUrl))
                {
                    return new ObjectResult(new { Code = 302, Location = model.ReturnUrl });
                }
                else if (string.IsNullOrWhiteSpace(model.ReturnUrl))
                {
                    return new ObjectResult(new { Code = 302, Location = "/" });
                }
                else
                {
                    return new ObjectResult(new
                    {
                        Code = Errors.InvalidReturnUrl,
                        Location = "/error.html?errorId=" + Errors.InvalidReturnUrl + "&returnUrl=" +
                                   model.ReturnUrl
                    });
                }
            }
            else
            {
                if (result.RequiresTwoFactor)
                {
                    return new ObjectResult(new
                    {
                        Code = Errors.IdentityTwoFactorIsNotSupported,
                        Location = "/error.html?errorId=" + Errors.IdentityTwoFactorIsNotSupported
                    });
                }

                if (result.IsNotAllowed)
                {
                    return new ObjectResult(new ApiResult { Code = Errors.IdentityUserIsNotAllowed, });
                }
                else if (result.IsLockedOut)
                {
                    return new ObjectResult(new ApiResult { Code = Errors.IdentityUserIsLockedOut, });
                }
                else
                {
                    await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials",
                        clientId: context?.Client.ClientId));
                    return new ObjectResult(new ApiResult { Code = Errors.IdentityInvalidCredentials, });
                }
            }

            // something went wrong, show form with error
            // var vm = await BuildLoginViewModelAsync(model);
            // return View(vm);
        }

        [HttpPost("SendSmsCode")]
        [EnableCors("configuration")]
        public async Task<ApiResult> SendPhoneNumber([FromBody] Inputs.V1.SendSmsCode input)
        {
            var code = RandomNumberGenerator.GetInt32(1111, 9999).ToString();
            await _phoneCodeStore.UpdateAsync(input.PhoneNumber, code);
            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation($"Send sms code to {input.PhoneNumber}: {code}");
                return new ApiResult() { Data = string.Empty, Message = string.Empty, Success = true, Code = 0 };
            }
            else
            {
                var smsClient = CreateClient();
                var countryCode = string.IsNullOrWhiteSpace(input.CountryCode) ? "+86" : input.CountryCode;
                var template = _aliyunSmsOptions.Templates.GetOrDefault(countryCode);
                if (string.IsNullOrEmpty(template))
                {
                    _logger.LogError($"CountryCode {countryCode} no sms template");
                    return new ApiResult() { Data = string.Empty, Message = "不支持的国家", Success = false, Code = 1 };
                }

                var sendSmsRequest =
                    new AlibabaCloud.SDK.Dysmsapi20170525.Models.SendSmsRequest
                    {
                        PhoneNumbers = $"{countryCode}{input.PhoneNumber}",
                        SignName = _aliyunSmsOptions.SignName,
                        TemplateCode = template,
                        TemplateParam = JsonSerializer.Serialize(new { code })
                    };

                try
                {
                    var response = await smsClient.SendSmsAsync(sendSmsRequest);
                    if (response.Body.Code == "OK")
                    {
                        return new ApiResult()
                        {
                            Data = string.Empty, Message = string.Empty, Success = true, Code = 0
                        };
                    }

                    _logger.LogError(response.Body.Message);
                    return new ApiResult() { Data = string.Empty, Message = "发送验证码失败", Success = false, Code = 1 };
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                    return new ApiResult() { Data = string.Empty, Message = "发送验证码失败", Success = false, Code = 1 };
                }
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
                await _signInManager.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
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

            var query =
                $"postLogoutRedirectUri={vm.PostLogoutRedirectUri}&clientName={vm.ClientName}&signOutIframeUrl={vm.SignOutIframeUrl}&automaticRedirectAfterSignOut={(vm.AutomaticRedirectAfterSignOut ? "true" : "false")}";

            return Redirect(
                $"~/loggedout.html?" + HttpUtility.UrlPathEncode(query));
        }

        private async Task<Outputs.V1.LoggedOutOutput> BuildLoggedOutOutputAsync(string logoutId)
        {
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

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

            vm.LogoutId ??= await _interaction.CreateLogoutContextAsync();
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

            var context = await _interaction.GetLogoutContextAsync(logoutId);
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
}
