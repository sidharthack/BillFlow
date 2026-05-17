namespace BillFlow.TenantService.DTOs;

public record TenantResponse(
    int Id,
    string Slug,
    string Name,
    string OwnerEmail,
    string Plan,
    string Status,
    DateTime CreatedAt,
    TenantSettingsResponse Settings
);

public record TenantSettingsResponse(
    string CompanyName,
    string? LogoUrl,
    string PrimaryColor,
    string Currency,
    string CountryCode,
    decimal DefaultTaxRate,
    string InvoicePrefix
);

public record RegisterTenantRequest(
    string Name,
    string OwnerEmail,
    string? CompanyName,
    string Currency = "INR",
    string CountryCode = "IN"
);