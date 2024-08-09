using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityTokenService.Controllers;

[SecurityHeaders]
[Route("[controller]")]
[Authorize]
public class DiagnosticsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new Outputs.V1.DiagnosticsOutput(await HttpContext.AuthenticateAsync());
        return new ObjectResult(model);
    }
}