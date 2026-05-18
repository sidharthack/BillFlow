using Microsoft.AspNetCore.Mvc;

namespace BillFlow.InvoiceService.Controllers;

// [ApiController] gives us: automatic model validation,
// automatic 400 responses, binding source inference
[ApiController]
[Route("[controller]")]  // Route = /health (controller name minus "Controller")
public class HealthController : ControllerBase
{
    // ControllerBase (not Controller) — no View support, API only

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