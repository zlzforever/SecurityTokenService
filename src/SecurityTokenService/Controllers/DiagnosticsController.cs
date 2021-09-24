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

            var model = new Outputs.V1.DiagnosticsOutput(await HttpContext.AuthenticateAsync());
            return new ObjectResult(model);
        }
    }
}