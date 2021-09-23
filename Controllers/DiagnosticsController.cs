using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SecurityTokenService.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class DiagnosticsController : ControllerBase
    {
        public async Task<IActionResult> Index()
        {
            var localAddresses = new List<string> { "127.0.0.1", "::1" };
            var ipAddress = HttpContext.Connection.LocalIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                localAddresses.Add(ipAddress);
            }

            if (!localAddresses.Contains(HttpContext.Connection.RemoteIpAddress?.ToString()))
            {
                return NotFound();
            }

            var model = new DiagnosticsViewModel(await HttpContext.AuthenticateAsync());
            return new ObjectResult(model);
        }
    }

    public class DiagnosticsViewModel
    {
        public DiagnosticsViewModel(AuthenticateResult result)
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
}