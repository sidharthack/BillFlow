using BillFlow.NotificationService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.NotificationService.Controllers;

[ApiController]
[Route("[controller]")]
public class NotificationLogController : ControllerBase
{
    private readonly NotificationDbContext _db;

    public NotificationLogController(NotificationDbContext db)
    {
        _db = db;
    }

    // GET /notificationlog?tenantId=1
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? tenantId)
    {
        var query = _db.NotificationLogs.AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(n => n.TenantId == tenantId.Value);

        var logs = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new
            {
                n.Id,
                n.TenantId,
                n.EventType,
                n.RecipientEmail,
                n.Subject,
                n.Status,
                n.RetryCount,
                n.CreatedAt,
                n.SentAt,
                n.ErrorMessage
            })
            .ToListAsync();

        return Ok(logs);
    }
}