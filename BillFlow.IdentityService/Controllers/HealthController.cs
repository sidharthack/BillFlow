using Microsoft.AspNetCore.Mvc;

namespace BillFlow.IdentityService.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "IdentityService",
        timestamp = DateTime.UtcNow
    });
}