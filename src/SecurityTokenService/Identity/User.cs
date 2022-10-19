using Microsoft.AspNetCore.Identity;

namespace SecurityTokenService.Identity;

public class User : IdentityUser
{
    public string FamilyName { get; set; }
    public string GivenName { get; set; }
    public string Picture { get; set; }

    public User(string userName)
        : base(userName)
    {
    }
}
