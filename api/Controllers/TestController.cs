using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockHub.Errors;
using StockHub.Interfaces;

namespace StockHub.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("Action1")]
    [AllowAnonymous]
    //[Authorize]
    //GET: api/Action1/Test
    //GET: api/Action1/Test/
    public ActionResult<string> Action1()
    {
        throw new SHArgumentException("Test");
        /*
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        if (identity != null)
        {
            IEnumerable<Claim> claims = identity.Claims;
            var ss = identity.Claims;
            return Ok(claims);
        }
        return Ok();*/
    }

    [HttpGet]
    [Route("[action]")]
    [Authorize]
    [ActionName("Action2")]
    public ActionResult<string> Action2([FromServices] IUserClaims userClaims)
    {
        var userId = userClaims.GetUid();
        return userId;
    }
    
    [HttpGet]
    [Route("[action]")]
    [AllowAnonymous]
    [ActionName("Hello")]
    public ActionResult Hello()
    {
        return Ok();
    }
}