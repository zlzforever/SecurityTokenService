using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;

namespace SecurityTokenService.Controllers
{
    public static class Outputs
    {
        public static class V1
        {
            public class ProcessConsentResult
            {
                public bool IsRedirect => RedirectUri != null;
                public string RedirectUri { get; set; }
                public string ClientId { get; set; }

                public bool ShowView => ViewModel != null;
                public object ViewModel { get; set; }

                public bool HasValidationError => ValidationError != null;
                public string ValidationError { get; set; }
            }

            public class ConsentOutput
            {
                public string ReturnUrl { get; set; }
                public string ClientName { get; set; }
                public string ClientUrl { get; set; }
                public string ClientLogoUrl { get; set; }
                public bool AllowRememberConsent { get; set; }

                public IEnumerable<ScopeOutput> IdentityScopes { get; set; }
                public IEnumerable<ScopeOutput> ResourceScopes { get; set; }
            }

            public class ScopeOutput
            {
                public string Name { get; set; }
                public string DisplayName { get; set; }
                public string Description { get; set; }
                public bool Emphasize { get; set; }
                public bool Required { get; set; }
                public bool Checked { get; set; }
            }

            public class DiagnosticsOutput
            {
                public DiagnosticsOutput(AuthenticateResult result)
                {
                    var claims = new List<object>();
                    if (result.Principal != null)
                    {
                        foreach (var claim in result.Principal.Claims)
                        {
                            claims.Add(new
                            {
                                claim.Type,
                                claim.Value
                            });
                        }

                        Claims = claims;
                    }

                    if (result.Properties != null)
                    {
                        var properties = new List<object>();
                        foreach (var prop in result.Properties.Items)
                        {
                            properties.Add(new
                            {
                                prop.Key,
                                prop.Value
                            });
                        }

                        Properties = properties;
                    }
                }

                public object Claims { get; }
                public object Properties { get; }
            }

            public class LogoutOutput
            {
                public string LogoutId { get; set; }
                public bool ShowLogoutPrompt { get; set; } = true;
            }

            public class LoggedOutOutput
            {
                public string PostLogoutRedirectUri { get; set; }
                public string ClientName { get; set; }
                public string SignOutIframeUrl { get; set; }

                public bool AutomaticRedirectAfterSignOut { get; set; }

                public string LogoutId { get; set; }
                public bool TriggerExternalSignout => ExternalAuthenticationScheme != null;
                public string ExternalAuthenticationScheme { get; set; }
            }
        }
    }
}