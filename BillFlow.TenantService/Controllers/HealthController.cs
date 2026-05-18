using Microsoft.AspNetCore.Mvc;

namespace BillFlow.TenantService.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "TenantService",
        timestamp = DateTime.UtcNow
    });
}