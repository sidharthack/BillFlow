using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.InvoiceService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "InvoiceService",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
