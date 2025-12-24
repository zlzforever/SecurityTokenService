namespace SecurityTokenService;

public class SecurityTokenServiceOptions
{
    /// <summary>
    /// 退出后是否自动跳转
    /// </summary>
    public bool AutomaticRedirectAfterSignOut { get; set; }

    /// <summary>
    /// 允许本地登录
    /// </summary>
    public bool AllowLocalLogin { get; set; }

    /// <summary>
    /// 允许记住登录
    /// </summary>
    public bool AllowRememberLogin { get; set; }

    /// <summary>
    /// 弹出确认退出的确认框
    /// </summary>
    public bool ShowLogoutPrompt { get; set; }

    /// <summary>
    /// 记住登录状态的时间间隔
    /// </summary>
    public int RememberMeLoginDuration { get; set; }

    public string WindowsAuthenticationSchemeName { get; set; }

    /// <summary>
    /// 验证码长度
    /// </summary>
    public int VerifyCodeLength { get; set; }

    /// <summary>
    /// 短信服务供应商
    /// </summary>
    public string SmsProvider { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool ForcePasswordSecurityPolicy { get; set; }

    public int GetVerifyCodeLength()
    {
        // 必须小于等于9, 否则整数会溢出
        return VerifyCodeLength <= 0 ? 4 : VerifyCodeLength >= 9 ? 9 : VerifyCodeLength;
    }
}
