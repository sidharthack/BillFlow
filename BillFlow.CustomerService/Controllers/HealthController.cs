using Microsoft.AspNetCore.Mvc;

namespace BillFlow.CustomerService.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "CustomerService",
        timestamp = DateTime.UtcNow
    });
}