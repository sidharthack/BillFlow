namespace BillFlow.InvoiceService.DTOs;

public record InvoiceResponse(
    int Id,
    string InvoiceNumber,
    int CustomerId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerGstNumber,
    decimal SubTotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? PaidAt,
    DateTime? DueDate,
    DateTime? CancelledAt,
    List<LineItemResponse> LineItems,
    List<InvoiceEventResponse> Events
);

public record LineItemResponse(
    int Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Amount
);

public record InvoiceEventResponse(
    string FromStatus,
    string ToStatus,
    string? Note,
    DateTime OccurredAt
);

// Request to create invoice — must include at least one line item
public record CreateInvoiceRequest(
    int CustomerId,
    List<CreateLineItemRequest> LineItems,
    string? Notes,
    DateTime? DueDate
);

public record CreateLineItemRequest(
    string Description,
    int Quantity,
    decimal UnitPrice
);

// Request to transition status
public record TransitionRequest(
    string ToStatus,
    string? Note
);