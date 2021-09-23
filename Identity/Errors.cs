namespace SecurityTokenService.Identity
{
    public static class Errors
    {
        public const int TwoFactorIsNotSupported = 4001;
        public const int UserIsNotAllowed = 4002;
        public const int UserIsLockedOut = 4003;
        public const int InvalidCredentials = 4004;
        public const int NativeClientIsNotSupported = 4005;
        public const int InvalidReturnUrl = 4006;
        
    }
}