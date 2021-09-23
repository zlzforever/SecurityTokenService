using System.ComponentModel.DataAnnotations;

namespace SecurityTokenService.Controllers
{
    public static class Inputs
    {
        public static class V1
        {
            public class LoginInput
            {
                /// <summary>
                /// 用户名
                /// </summary>
                [Required(ErrorMessage = "用户名不能为空")]
                public string Username { get; set; }

                /// <summary>
                /// 密码
                /// </summary>
                [Required(ErrorMessage = "密码不能为空")]
                public string Password { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [Display(Name = "记住我")]
                public bool RememberLogin { get; set; }

                public string ReturnUrl { get; set; }

                public string Button { get; set; }
            }
            
            public class LogoutInput
            {
                public string LogoutId { get; set; }
            }
        }
    }
}