using BillFlow.InvoiceService.DTOs;
using BillFlow.InvoiceService.Models;
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
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        IInvoiceService invoiceService,
        ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    // All authenticated users can list invoices
    [HttpGet]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _invoiceService.GetAllAsync();
        var response = invoices.Select(MapToResponse);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanRead")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice is null)
            return NotFound(new { message = $"Invoice {id} not found" });
        return Ok(MapToResponse(invoice));
    }

    // Only Admins and Members can create
    [HttpPost]
    [Authorize(Policy = "CanWrite")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CustomerName = request.CustomerName,
            Amount = request.Amount,
            TaxRate = request.TaxRate,
            Status = InvoiceStatus.Draft
        };

        var created = await _invoiceService.CreateAsync(invoice);
        return CreatedAtAction(nameof(GetById),
            new { id = created.Id }, MapToResponse(created));
    }

    // Only Admins can cancel
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Cancel(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice is null)
            return NotFound(new { message = $"Invoice {id} not found" });

        await _invoiceService.CancelAsync(id);
        return NoContent();
    }

    private static InvoiceResponse MapToResponse(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.CustomerName,
        i.Amount, i.TaxRate, i.TotalAmount,
        i.Status.ToString(), i.CreatedAt);
}