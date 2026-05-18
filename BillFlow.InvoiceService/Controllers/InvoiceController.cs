using BillFlow.InvoiceService.DTOs;
using BillFlow.InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.InvoiceService.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _invoiceService.GetAllAsync();
        return Ok(invoices);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice is null)
            return NotFound(new { message = $"Invoice {id} not found" });
        return Ok(invoice);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        // Extract bearer token to forward to CustomerService
        var bearerToken = HttpContext.Request.Headers["Authorization"]
            .FirstOrDefault()
            ?.Replace("Bearer ", "") ?? string.Empty;

        try
        {
            var invoice = await _invoiceService.CreateAsync(request, bearerToken);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST /invoice/5/transition  { "toStatus": "Sent", "note": "optional" }
    [HttpPost("{id:int}/transition")]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> Transition(
        int id, [FromBody] TransitionRequest request)
    {
        try
        {
            var invoice = await _invoiceService.TransitionAsync(id, request);
            if (invoice is null)
                return NotFound(new { message = $"Invoice {id} not found" });
            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _invoiceService.CancelAsync(id);
        return NoContent();
    }
}