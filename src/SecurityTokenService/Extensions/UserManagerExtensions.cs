using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Extensions;

public static class UserManagerExtensions
{
    public static async Task<User> FindAsync(this UserManager<User> userManager,
        string loginInput, string softDeleteColumn = null)
    {
        User user;
        if (string.IsNullOrWhiteSpace(softDeleteColumn))
        {
            user = await userManager.Users.FirstOrDefaultAsync(x =>
                x.UserName == loginInput || x.Email == loginInput ||
                x.PhoneNumber == loginInput);
        }
        else
        {
            user = await userManager.Users
                .FirstOrDefaultAsync(x =>
                    EF.Property<bool>(x, softDeleteColumn) == false &&
                    (x.UserName == loginInput || x.Email == loginInput ||
                     x.PhoneNumber == loginInput));
        }

        return user;
    }
}
