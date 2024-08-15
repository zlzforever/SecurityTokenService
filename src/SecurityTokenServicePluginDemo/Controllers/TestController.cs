using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityTokenServicePluginDemo.Controllers;

[Route("[controller]")]
[AllowAnonymous]
public class TestController
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        return new ObjectResult("OK");
    }
}
