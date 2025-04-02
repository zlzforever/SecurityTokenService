namespace SecurityTokenService;

public class SecurityTokenServiceOptions
{
    public bool AutomaticRedirectAfterSignOut { get; set; }
    public bool AllowLocalLogin { get; set; }
    public bool AllowRememberLogin { get; set; }
    public bool ShowLogoutPrompt { get; set; }
    public int RememberMeLoginDuration { get; set; }
    public string WindowsAuthenticationSchemeName { get; set; }
    public int SmsCodeLength { get; set; }

    public int GetSmsCodeNumberLength()
    {
        // 必须小于等于9,否则整数会溢出
        return SmsCodeLength <= 0 ? 4 : SmsCodeLength >= 9 ? 9 : SmsCodeLength;
    }
}
