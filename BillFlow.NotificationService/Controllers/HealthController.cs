using Microsoft.AspNetCore.Mvc;

namespace BillFlow.NotificationService.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "NotificationService",
        timestamp = DateTime.UtcNow
    });
}