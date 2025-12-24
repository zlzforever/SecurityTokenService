using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SecurityTokenService.Identity;

namespace SecurityTokenService.Extensions;

public static class UserManagerExtensions
{
    //根据手机号获取用户信息
    public static async Task<User> GetUserByPhoneNumberAsync(this UserManager<User> userManager, string phoneNumber,
        string softDeleteColumn = null)
    {
        User user;
        if (string.IsNullOrWhiteSpace(softDeleteColumn))
        {
            user = await userManager.Users.FirstOrDefaultAsync(x => x.UserName == phoneNumber ||
                                                                    x.PhoneNumber == phoneNumber);
        }
        else
        {
            user = await userManager.Users
                .FirstOrDefaultAsync(x =>
                    EF.Property<bool>(x, softDeleteColumn) == false &&
                    (x.UserName == phoneNumber ||
                     x.PhoneNumber == phoneNumber));
        }

        return user;
    }

    /// <summary>
    /// 只查找第一个符合条件的用户
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="loginInput"></param>
    /// <param name="softDeleteColumn"></param>
    /// <returns></returns>
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
