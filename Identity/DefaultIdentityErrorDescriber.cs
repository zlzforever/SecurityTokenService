using Microsoft.AspNetCore.Identity;

namespace SecurityTokenService.Identity
{
    public class SecurityTokenServiceIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError()
        {
            return new() { Code = nameof(DefaultError), Description = "未知错误！" };
        }

        public override IdentityError ConcurrencyFailure()
        {
            return new() { Code = nameof(ConcurrencyFailure), Description = "并发错误，对象已被修改！" };
        }

        public override IdentityError PasswordMismatch()
        {
            return new() { Code = "Password", Description = "密码错误！" };
        }

        public override IdentityError InvalidToken()
        {
            return new() { Code = nameof(InvalidToken), Description = "Invalid token." };
        }

        public override IdentityError LoginAlreadyAssociated()
        {
            return new() { Code = nameof(LoginAlreadyAssociated), Description = "当前用户已经登录！" };
        }

        public override IdentityError InvalidUserName(string userName)
        {
            return new() { Code = "UserName", Description = $"用户名 '{userName}' 错误，只可以包含数字和字母！" };
        }

        public override IdentityError InvalidEmail(string email)
        {
            return new() { Code = "Email", Description = $"邮箱 '{email}' 格式错误！" };
        }

        public override IdentityError DuplicateUserName(string userName)
        {
            return new() { Code = "UserName", Description = $"用户名 '{userName}' 已存在！" };
        }

        public override IdentityError DuplicateEmail(string email)
        {
            return new() { Code = "Email", Description = $"邮箱 '{email}' 已经存在！" };
        }

        public override IdentityError InvalidRoleName(string role)
        {
            return new() { Code = nameof(InvalidRoleName), Description = $"角色 '{role}' 验证错误！" };
        }

        public override IdentityError DuplicateRoleName(string role)
        {
            return new() { Code = nameof(DuplicateRoleName), Description = $"角色名 '{role}' 已经存在！" };
        }

        public override IdentityError UserAlreadyHasPassword()
        {
            return new() { Code = nameof(UserAlreadyHasPassword), Description = "User already has a password set." };
        }

        public override IdentityError UserLockoutNotEnabled()
        {
            return new()
                { Code = nameof(UserLockoutNotEnabled), Description = "Lockout is not enabled for this user." };
        }

        public override IdentityError UserAlreadyInRole(string role)
        {
            return new() { Code = nameof(UserAlreadyInRole), Description = $"User already in role '{role}'." };
        }

        public override IdentityError UserNotInRole(string role)
        {
            return new() { Code = nameof(UserNotInRole), Description = $"User is not in role '{role}'." };
        }

        public override IdentityError PasswordTooShort(int length)
        {
            return new() { Code = "Password", Description = $"密码至少 {length} 位！" };
        }

        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new() { Code = "Password", Description = "密码必须至少有一个非字母数字字符." };
        }

        public override IdentityError PasswordRequiresDigit()
        {
            return new() { Code = "Password", Description = "密码至少有一个数字 ('0'-'9')." };
        }

        public override IdentityError PasswordRequiresLower()
        {
            return new() { Code = "Password", Description = "密码必须包含小写字母 ('a'-'z')." };
        }

        public override IdentityError PasswordRequiresUpper()
        {
            return new() { Code = "Password", Description = "密码必须包含大写字母 ('A'-'Z')." };
        }
    }
}