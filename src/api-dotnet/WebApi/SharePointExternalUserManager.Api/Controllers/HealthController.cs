using Microsoft.AspNetCore.Mvc;

namespace SharePointExternalUserManager.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "Healthy", version = "1.0.0", timestamp = DateTime.UtcNow });
}
