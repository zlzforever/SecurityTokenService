using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityTokenServicePluginDemo.Controllers;

[Route("[controller]")]
[AllowAnonymous]
public class TestController
{
    [HttpGet]
    public Task<IActionResult> GetAsync()
    {
        return Task.FromResult<IActionResult>(new ObjectResult("OK"));
    }
}
