using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityTokenService.Identity;
using SecurityTokenService.Utils;

namespace SecurityTokenService.IdentityServer;

public class ResourceOwnerPasswordValidator(
    IOptionsMonitor<IdentityExtensionOptions> identityExtensionOptions,
    UserManager<User> userManager,
    IMemoryCache memoryCache,
    ILogger<ResourceOwnerPasswordValidator> logger,
    IHttpContextAccessor contextAccessor,
    SignInManager<User> signInManager)
    : IResourceOwnerPasswordValidator
{
    private readonly IdentityExtensionOptions _identityExtensionOptions = identityExtensionOptions.CurrentValue;

    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        if (contextAccessor.HttpContext == null)
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidRequest,
                "invalid_request");
            return;
        }

        var request = contextAccessor.HttpContext.Request;
        var captchaCode = context.Request.Raw["CaptchaCode"];
        var checkCaptchaResult = Util.CheckCaptcha(memoryCache, logger, request, captchaCode);
        if (checkCaptchaResult != null)
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidRequest,
                "invalid_captcha_code");
            return;
        }

        User user;
        if (string.IsNullOrWhiteSpace(_identityExtensionOptions.SoftDeleteColumn))
        {
            user = await userManager.Users.FirstOrDefaultAsync(x =>
                x.UserName == context.UserName || x.Email == context.UserName ||
                x.PhoneNumber == context.UserName);
        }
        else
        {
            user = await userManager.Users
                .FirstOrDefaultAsync(x =>
                    EF.Property<bool>(x, _identityExtensionOptions.SoftDeleteColumn) == false &&
                    (x.UserName == context.UserName || x.Email == context.UserName ||
                     x.PhoneNumber == context.UserName));
        }

        if (user == null)
        {
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "invalid_username");
            return;
        }

        var result = await signInManager.PasswordSignInAsync(user, context.Password,
            false, false);
        if (result.Succeeded)
        {
            context.Result = new GrantValidationResult(
                user.Id,
                OidcConstants.AuthenticationMethods.Password);
            return;
        }

        context.Result = new GrantValidationResult(
            TokenRequestErrors.InvalidGrant,
            "invalid_password");
    }
}
