﻿using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SecurityTokenService.Identity;

namespace SecurityTokenService.IdentityServer;

public class ProfileService : IdentityServer4.AspNetIdentity.ProfileService<User>
{
    public ProfileService(UserManager<User> userManager,
        IUserClaimsPrincipalFactory<User> claimsFactory) : base(userManager, claimsFactory)
    {
    }

    public ProfileService(UserManager<User> userManager,
        IUserClaimsPrincipalFactory<User> claimsFactory,
        ILogger<IdentityServer4.AspNetIdentity.ProfileService<User>> logger) : base(userManager, claimsFactory, logger)
    {
    }

    protected override async Task<ClaimsPrincipal> GetUserClaimsAsync(User user)
    {
        var principal = await base.GetUserClaimsAsync(user);
        var identity = new ClaimsIdentity();
        
        if (!principal.HasClaim(x => x.Type == JwtClaimTypes.GivenName))
        {
            identity.AddClaim(new Claim(JwtClaimTypes.GivenName,
                string.IsNullOrWhiteSpace(user.GivenName) ? string.Empty : user.GivenName));
        }

        if (!principal.HasClaim(x => x.Type == JwtClaimTypes.FamilyName))
        {
            identity.AddClaim(new Claim(JwtClaimTypes.FamilyName,
                string.IsNullOrWhiteSpace(user.FamilyName) ? string.Empty : user.FamilyName));
        }

        if (!principal.HasClaim(x => x.Type == JwtClaimTypes.Picture))
        {
            identity.AddClaim(new Claim(JwtClaimTypes.Picture,
                string.IsNullOrWhiteSpace(user.Picture) ? string.Empty : user.Picture));
        }

        principal.AddIdentity(identity);
        return principal;
    }
}
