namespace SecurityTokenService.Identity
{
    public class IdentityExtensionOptions
    {
        public string SoftDeleteColumn { get; set; }
        public Tables Tables { get; set; }
        public string TablePrefix { get; set; }
    }

    public class Tables
    {
        public string User { get; set; }
        public string Role { get; set; }
        public string UserRole { get; set; }
        public string RoleClaim { get; set; }
        public string UserClaim { get; set; }
        public string UserLogin { get; set; }
        public string UserToken { get; set; }
    }
}