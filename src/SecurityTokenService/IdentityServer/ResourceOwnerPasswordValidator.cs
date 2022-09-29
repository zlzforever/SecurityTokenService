using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SecurityTokenService.Identity;

namespace SecurityTokenService.IdentityServer;

public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly IdentityExtensionOptions _identityExtensionOptions;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    
    public ResourceOwnerPasswordValidator(
        IOptionsMonitor<IdentityExtensionOptions> identityExtensionOptions,
        UserManager<User> userManager,
        SignInManager<User> signInManager
        )
    {
        _identityExtensionOptions = identityExtensionOptions.CurrentValue;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        User user;
        if (string.IsNullOrWhiteSpace(_identityExtensionOptions.SoftDeleteColumn))
        {
            user = await _userManager.Users.FirstOrDefaultAsync(x =>
                x.UserName == context.UserName || x.Email == context.UserName ||
                x.PhoneNumber == context.UserName);
        }
        else
        {
            user = await _userManager.Users
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
        
        var result = await _signInManager.PasswordSignInAsync(user, context.Password,
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