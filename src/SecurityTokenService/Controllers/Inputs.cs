using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace SecurityTokenService.Controllers
{
    public static class Inputs
    {
        public static class V1
        {
            public class ResetPasswordByOldPasswordInput
            {
                /// <summary>
                /// 用户名或邮箱或手机号
                /// </summary>
                [Required, StringLength(50)]
                public string UserName { get; set; }

                /// <summary>
                /// 新密码
                /// </summary>
                [Required, StringLength(32)]
                public string NewPassword { get; set; }

                /// <summary>
                /// 确认新密码
                /// </summary>
                [Required, StringLength(32)]
                [Compare("NewPassword", ErrorMessage = "两次密码不一致")]
                public string ConfirmNewPassword { get; set; }

                /// <summary>
                /// 旧密码
                /// </summary>
                [Required, StringLength(50)]
                public string OldPassword { get; set; }
            }

            public class ResetPasswordByPhoneNumberInput
            {
                /// <summary>
                /// 新密码
                /// </summary>
                [StringLength(36, MinimumLength = 4, ErrorMessage = "密码长度不规范")]
                public string NewPassword { get; set; }

                /// <summary>
                /// 确认新密码
                /// </summary>
                [StringLength(36, MinimumLength = 4, ErrorMessage = "密码长度不规范")]
                [Compare("NewPassword", ErrorMessage = "两次密码不一致")]
                public string ConfirmNewPassword { get; set; }

                /// <summary>
                /// 手机号
                /// </summary>
                [Required(ErrorMessage = "手机号能不能空"), StringLength(20, ErrorMessage = "手机号长度超长")]
                public string PhoneNumber { get; set; }

                /// <summary>
                /// 验证码
                /// </summary>
                [Required(ErrorMessage = "请填写验证码"), StringLength(6, ErrorMessage = "验证码长度不正确")]
                public string VerifyCode { get; set; }
            }

            public class SendSmsCode
            {
                /// <summary>
                /// 
                /// </summary>
                [Required(ErrorMessage = "手机号能不能空"), StringLength(20, ErrorMessage = "手机号长度超长")]
                public string PhoneNumber { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [StringLength(10, ErrorMessage = "国家地区码长度不正确")]
                public string CountryCode { get; set; }
            }

            public class ConsentInput
            {
                /// <summary>
                /// 
                /// </summary>
                [StringLength(10)]
                public string Button { get; set; }

                public IEnumerable<string> ScopesConsented { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [StringLength(10)]
                public string RememberConsent { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [StringLength(1000)]
                public string ReturnUrl { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [StringLength(2000)]
                public string Description { get; set; }
            }

            public class LoginBySmsInput
            {
                /// <summary>
                /// 
                /// </summary>
                [Required(ErrorMessage = "手机号能不能空"), StringLength(20, ErrorMessage = "手机号长度超长")]
                public string PhoneNumber { get; set; }

                /// <summary>
                /// 验证码
                /// </summary>
                [Required(ErrorMessage = "请填写验证码"), StringLength(6, ErrorMessage = "验证码长度不正确")]
                public string VerifyCode { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [Display(Name = "记住我")]
                public bool RememberLogin { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [StringLength(1000)]
                public string ReturnUrl { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [StringLength(5)]
                public string Button { get; set; }
            }

            public class LoginInput
            {
                /// <summary>
                /// 用户名
                /// </summary>
                [Required(ErrorMessage = "用户名不能为空"),
                 StringLength(24, ErrorMessage = "用户名不符合规范")]
                public string Username { get; set; }

                /// <summary>
                /// 密码
                /// </summary>
                [Required(ErrorMessage = "密码不能为空"),
                 StringLength(24, ErrorMessage = "密码不符合规范")]
                public string Password { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [Display(Name = "记住我")]
                public bool RememberLogin { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [StringLength(1000)]
                public string ReturnUrl { get; set; }

                /// <summary>
                /// 
                /// </summary>
                [StringLength(5)]
                public string Button { get; set; }
            }

            public class LogoutInput
            {
                /// <summary>
                /// 
                /// </summary>
                [StringLength(36)]
                public string LogoutId { get; set; }
            }
        }
    }
}
