using Microsoft.AspNetCore.Mvc;

namespace Aristokeides.Api.Controllers;

public class RootController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/dashboard");
        }
        return Redirect("/home");
    }
}
