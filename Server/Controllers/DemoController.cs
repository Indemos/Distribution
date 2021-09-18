using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers;

[ApiController]
public class DemoController : ControllerBase
{
  [HttpGet]
  [Route("")]
  public string Get()
  {
    return "Demo";
  }
}
