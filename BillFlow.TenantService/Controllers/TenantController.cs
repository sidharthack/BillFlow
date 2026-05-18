using BillFlow.TenantService.DTOs;
using BillFlow.TenantService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.TenantService.Controllers;

[ApiController]
[Route("[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _tenantService.GetAllAsync();
        return Ok(tenants);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var tenant = await _tenantService.GetBySlugAsync(slug);

        if (tenant is null)
            return NotFound(new { message = $"Tenant '{slug}' not found or inactive" });

        return Ok(tenant);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Tenant name is required" });

        if (string.IsNullOrWhiteSpace(request.OwnerEmail))
            return BadRequest(new { message = "Owner email is required" });

        var tenant = await _tenantService.RegisterAsync(request);

        return CreatedAtAction(
            nameof(GetBySlug),
            new { slug = tenant.Slug },
            tenant);
    }

    [HttpGet("exists/{slug}")]
    public async Task<IActionResult> Exists(string slug)
    {
        var exists = await _tenantService.ExistsAsync(slug);
        return Ok(new { slug, exists });
    }
}