using BillFlow.InvoiceService.DTOs;
using BillFlow.InvoiceService.Models;
using BillFlow.InvoiceService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillFlow.InvoiceService.Controllers;

[Authorize]           // ← every endpoint in this controller requires a valid JWT
[ApiController]
[Route("[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(IInvoiceService invoiceService, ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _invoiceService.GetAllAsync();

        var response = invoices.Select(i => new InvoiceResponse(
            i.Id,
            i.InvoiceNumber,
            i.CustomerName,
            i.Amount,
            i.TaxRate,
            i.TotalAmount,  
            i.Status.ToString(),
            i.CreatedAt
        ));

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);

        if (invoice is null)
            return NotFound(new { message = $"Invoice {id} not found" });

        return Ok(new InvoiceResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerName,
            invoice.Amount,
            invoice.TaxRate,
            invoice.TotalAmount,
            invoice.Status.ToString(),
            invoice.CreatedAt
        ));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request)
    {
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerName = request.CustomerName,
            Amount = request.Amount,
            TaxRate = request.TaxRate,
            Status = InvoiceStatus.Draft
        };

        var created = await _invoiceService.CreateAsync(invoice);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            new InvoiceResponse(
                created.Id,
                created.InvoiceNumber,
                created.CustomerName,
                created.Amount,
                created.TaxRate,
                created.TotalAmount,
                created.Status.ToString(),
                created.CreatedAt
            )
        );
    }
}