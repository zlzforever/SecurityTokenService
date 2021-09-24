using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityTokenService.Controllers
{
    [Route("[controller]")]
    [SecurityHeaders]
    [Authorize]
    public class SessionController : ControllerBase
    {
        [HttpGet]
        public ValueTask<IActionResult> IndexAsync()
        {
            IActionResult a = new ObjectResult(new ApiResult
            {
                Code = 200,
                Data = HttpContext.User.Claims.Where(x => x.Type != "AspNet.Identity.SecurityStamp")
                    .ToDictionary(x => x.Type, x => x.Value)
            });
            return ValueTask.FromResult(a);
        }
    }
}