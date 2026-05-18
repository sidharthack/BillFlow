using BillFlow.CustomerService.DTOs;
using BillFlow.CustomerService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.CustomerService.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // GET /customer
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    // GET /customer/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is null)
            return NotFound(new { message = $"Customer {id} not found" });
        return Ok(customer);
    }

    // GET /customer/by-email/raj@acme.com
    [HttpGet("by-email/{email}")]
    public async Task<IActionResult> GetByEmail(string email)
    {
        var customer = await _customerService.GetByEmailAsync(email);
        if (customer is null)
            return NotFound(new { message = $"Customer with email '{email}' not found" });
        return Ok(customer);
    }

    // POST /customer
    [HttpPost]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Customer name is required" });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Customer email is required" });

        try
        {
            var customer = await _customerService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT /customer/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var updated = await _customerService.UpdateAsync(id, request);
        if (updated is null)
            return NotFound(new { message = $"Customer {id} not found" });
        return Ok(updated);
    }

    // DELETE /customer/5  (soft delete — sets status to Inactive)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var success = await _customerService.DeactivateAsync(id);
        if (!success)
            return NotFound(new { message = $"Customer {id} not found" });
        return NoContent();
    }
}