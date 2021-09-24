namespace SecurityTokenService
{
    public class SecurityTokenServiceOptions
    {
        public bool AutomaticRedirectAfterSignOut { get; set; }
        public bool AllowLocalLogin { get; set; }
        public bool AllowRememberLogin { get; set; }
        public bool ShowLogoutPrompt { get; set; }
        public int RememberMeLoginDuration { get; set; }
        public string WindowsAuthenticationSchemeName { get; set; }
    }
}