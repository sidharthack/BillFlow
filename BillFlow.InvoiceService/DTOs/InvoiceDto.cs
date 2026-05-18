namespace BillFlow.InvoiceService.DTOs;

// What comes OUT of the API (response)
public record InvoiceResponse(
    int Id,
    string InvoiceNumber,
    string CustomerName,
    decimal Amount,
    decimal TaxRate,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt
);

// What comes IN to create an invoice (request body)
public record CreateInvoiceRequest(
    string CustomerName,
    decimal Amount,
    decimal TaxRate
);