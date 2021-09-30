using System.Linq;
using System.Threading.Tasks;
using System.Web;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SecurityTokenService.Data;
using SecurityTokenService.Extensions;

namespace SecurityTokenService.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly SecurityTokenServiceDbContext _dbContext;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly SecurityTokenServiceOptions _options;
        private readonly IClientStore _clientStore;

        public AccountController(IIdentityServerInteractionService interaction, IEventService events,
            SecurityTokenServiceDbContext dbContext,
            SignInManager<IdentityUser> signInManager, IOptionsMonitor<SecurityTokenServiceOptions> options,
            IClientStore clientStore)
        {
            _interaction = interaction;
            _events = events;
            _dbContext = dbContext;
            _signInManager = signInManager;
            _clientStore = clientStore;
            _options = options.CurrentValue;
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
                            Code = Errors.IdentityNativeClientIsNotSupported,
                            Message = "不支持 NativeClient"
                        });
                    }

                    return new ObjectResult(new
                    {
                        Code = 302,
                        Location = model.ReturnUrl
                    });
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return new ObjectResult(new
                    {
                        Code = 302,
                        Location = "/"
                    });
                }
            }

            if (ModelState.IsValid)
            {
                var user = await _dbContext.Users
                    .FromSqlRaw(Constants.LoginUserQuerySql, model.Username)
                    .OrderBy(x => x.Id)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials",
                        clientId: context?.Client.ClientId));
                    return new ObjectResult(new ApiResult
                    {
                        Code = Errors.IdentityInvalidCredentials,
                        Message = "用户名或密码不正确"
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
                            return Redirect("~/redirect.html?redirectUrl=" + HttpUtility.UrlEncode(model.ReturnUrl));
                        }
                        else
                        {
                            // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                            return new ObjectResult(new
                            {
                                Code = 302,
                                Location = model.ReturnUrl
                            });
                        }
                    }

                    // request for a local page
                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return new ObjectResult(new
                        {
                            Code = 302,
                            Location = model.ReturnUrl
                        });
                    }
                    else if (string.IsNullOrEmpty(model.ReturnUrl))
                    {
                        return new ObjectResult(new
                        {
                            Code = 302,
                            Location = "/"
                        });
                    }
                    else
                    {
                        return new ObjectResult(new
                        {
                            Code = Errors.InvalidReturnUrl,
                            Location = "/error.html?errorId=" + Errors.InvalidReturnUrl
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
                        return new ObjectResult(new ApiResult
                        {
                            Code = Errors.IdentityUserIsNotAllowed,
                        });
                    }
                    else if (result.IsLockedOut)
                    {
                        return new ObjectResult(new ApiResult
                        {
                            Code = Errors.IdentityUserIsLockedOut,
                        });
                    }
                    else
                    {
                        await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials",
                            clientId: context?.Client.ClientId));
                        return new ObjectResult(new ApiResult
                        {
                            Code = Errors.IdentityInvalidCredentials,
                        });
                    }
                }
            }

            // something went wrong, show form with error
            // var vm = await BuildLoginViewModelAsync(model);
            // return View(vm);
            return new ObjectResult(new
            {
                Code = 302,
                Location = "/error.html?errorId=" + Errors.IdentityLoginFailed
            });
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout(string logoutId)
        {
            var vm = await BuildLogoutOutputAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(new Inputs.V1.LogoutInput
                {
                    LogoutId = logoutId
                });
            }

            return Redirect($"~/logout.html?logoutId={vm.LogoutId}");
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromForm] Inputs.V1.LogoutInput model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutOutputAsync(model.LogoutId);

            if (User?.Identity?.IsAuthenticated == true)
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

            if (User?.Identity?.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

        private async Task<Outputs.V1.LogoutOutput> BuildLogoutOutputAsync(string logoutId)
        {
            var vm = new Outputs.V1.LogoutOutput()
                { LogoutId = logoutId, ShowLogoutPrompt = _options.ShowLogoutPrompt };

            if (User?.Identity?.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }
    }
}