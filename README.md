dotnet ef migrations add SecurityTokenServiceInit --context PostgreSqlSecurityTokenServiceDbContext --output-dir
Data/PostgreSql/Migrations/SecurityTokenService
dotnet ef migrations add PersistedGrantInit --context PostgreSqlPersistedGrantDbContext --output-dir
Data/PostgreSql/Migrations/PersistedGrant
dotnet ef migrations add DataProtectionTable --context PostgreSqlSecurityTokenServiceDbContext --output-dir
Data/PostgreSql/Migrations/SecurityTokenService

dotnet ef migrations add SecurityTokenServiceInit --context MySqlSecurityTokenServiceDbContext --output-dir
Data/MySql/Migrations/SecurityTokenService
dotnet ef migrations add PersistedGrantInit --context MySqlPersistedGrantDbContext --output-dir
Data/MySql/Migrations/PersistedGrant

### 变更

20251223

+ 相同手机号发码间隔 60s 
+ 同一个验证码只能使用一次
+ sms_phone_code 不再使用
+ 配置验证时效
```
  "DataProtectionTokenProviderOptions": {
    "TokenLifespan": 60
  }
```
+ 需要配置 Lockout 相关， 登录失败 N 次直接锁定， 再也无法登录 
```
  "Identity": {
    "Lockout": {
      "DefaultLockoutTimeSpan": "00:03:00",
      "MaxFailedAccessAttempts": 5,
      "AllowedForNewUsers": true
    }
  }
```
+ 现在示例项目的 HTML 是一个 Tab 两个 Form， 无法使用两个同名的验证码字段。使用短信登录时， 需要前端点发送验证码的时候， 弹出输入验证码， 然后再构造请求登录。
+ api/v1.0/captcha 这个路径下是生成图形验证码的相关接口，需要在网关上做源 IP 限流.
+ 配置文件中开启密码策略， 测试：使用原密码修改新密码、使用手机验证码更新密码是否会强制密码策略， 接口调整为：
   /account/resetPwdByOriginPwd, /account/resetPwd
   此界面 HTML/JS 要调整添加验证码

```
  "Identity": {
    "Password": {
      "RequireDigit": true,
      "RequireLowercase": true,
      "RequiredLength": 8,
      "RequireNonAlphanumeric": true,
      "RequireUppercase": true,
      "RequiredUniqueChars": 1
    }
  }
```
+ 配置文件设置 SecurityTokenService:ForcePasswordSecurityPolicy， 如果用户密码不符合要求， 不允许登录
+ 发送验证码请求体变化 POST /account/sendCode

```
{
 "PhoneNumber": "",
 "PhoneNumber": "+86",
 "Scenario": "Login | ResetPassword | Register",
 "CaptchaCode": "图形验证码"
}
```
+ 

20230309

1. 通过原密码修改新密码
2. 移除记录密码强弱信息， 通过前端、后端密码策略配置来限制登录
3. 重构代码
4. 程序启动后判断是否 HTML 含有 site.js， 若有则替换为 site.min.js
5. IdentityServer TablePrefix 独立配置