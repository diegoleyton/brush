using Microsoft.AspNetCore.Mvc;

namespace Brush.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get() =>
        Ok(new HealthResponse(Status: "ok", Service: "brush-api"));
}
