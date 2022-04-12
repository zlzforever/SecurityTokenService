// using System.ComponentModel.DataAnnotations;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Authentication;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
//
// namespace SecurityTokenService.Controllers;
//
// public class LoginCommand
// {
//     /// <summary>
//     /// 用户名
//     /// </summary>
//     [Required(ErrorMessage = "请输入账号")]
//     public string Username { get; set; }
//
//     /// <summary>
//     /// 密码
//     /// </summary>
//     [Required(ErrorMessage = "请输入密码")]
//     public string Password { get; set; }
//
//     public bool RememberLogin { get; set; }
//
//     public string ReturnUrl { get; set; }
// }
//
// [Route("api/v1.0/account")]
// public class AccountApiController : Controller
// {
//     private readonly UserManager<IdentityUser> _userManager;
//     private readonly SignInManager<IdentityUser> _signInManager;
//
//     public AccountApiController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
//     {
//         _userManager = userManager;
//         _signInManager = signInManager;
//     }
//
//     [HttpPost("Login")]
//     [AllowAnonymous]
//     public async Task<IActionResult> Login([FromBody] LoginCommand model)
//     {
//         var user = await _userManager.FindByNameAsync(model.Username);
//
//         if (user == null)
//         {
//             return Json(new
//             {
//                 msg = "用户名或密码错误",
//                 success = false,
//                 code = 1
//             });
//         }
//
//         var result = await _signInManager.PasswordSignInAsync(user, model.Password,
//             model.RememberLogin, false);
//         if (result.Succeeded)
//         {
//             if (!string.IsNullOrWhiteSpace(model.ReturnUrl))
//             {
//                 return Json(new
//                 {
//                     msg = "",
//                     success = true,
//                     code = 0,
//                     data = new
//                     {
//                         returnUrl = model.ReturnUrl
//                     }
//                 });
//             }
//             else
//             {
//                 return Json(new
//                 {
//                     msg = "",
//                     success = true,
//                     code = 0,
//                     data = new
//                     {
//                         id = user.Id,
//                         name = user.UserName
//                     }
//                 });
//             }
//         }
//
//         if (result.IsLockedOut)
//         {
//             return Json(new
//             {
//                 msg = "帐号被锁定",
//                 success = false,
//                 code = 2
//             });
//         }
//         else
//         {
//             return Json(new
//             {
//                 msg = "用户名或密码错误",
//                 success = false,
//                 code = 3
//             });
//         }
//     }
//
//     [HttpPost("Logout")]
//     // [Authorize]
//     public async Task Logout()
//     {
//         await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
//         await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
//         // await _signInManager.SignOutAsync();
//     }
// }