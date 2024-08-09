using System.Linq;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using SecurityTokenService.Identity;
using Serilog;

namespace SecurityTokenService.Data;

public class SeedData
{
    private readonly UserManager<User> _userManager;

    public SeedData(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public void Load()
    {
        if (!_userManager.Users.Any())
        {
            foreach (var user in TestUsers.Users)
            {
                var result = _userManager
                    .CreateAsync(new User(user.Username), user.Password)
                    .Result;
                if (!result.Succeeded)
                {
                    Log.Logger.Error(
                        $"Create user: {user.Username} failed: {JsonConvert.SerializeObject(result.Errors)}");
                }
            }
        }
    }
}