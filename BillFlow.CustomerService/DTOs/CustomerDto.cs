namespace BillFlow.CustomerService.DTOs;

public record CustomerResponse(
    int Id,
    string Name,
    string Email,
    string? Phone,
    string? GstNumber,
    string? PanNumber,
    string Status,
    DateTime CreatedAt,
    AddressResponse? Address
);

public record AddressResponse(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PinCode,
    string Country
);

public record CreateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? GstNumber,
    string? PanNumber,
    CreateAddressRequest? Address
);

public record CreateAddressRequest(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PinCode,
    string Country = "India"
);

public record UpdateCustomerRequest(
    string Name,
    string? Phone,
    string? GstNumber,
    string? PanNumber,
    CreateAddressRequest? Address
);