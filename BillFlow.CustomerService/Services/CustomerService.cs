using BillFlow.Contracts.Tenancy;
using BillFlow.CustomerService.Data;
using BillFlow.CustomerService.DTOs;
using BillFlow.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.CustomerService.Services;

public class CustomerService : ICustomerService
{
    private readonly CustomerDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        CustomerDbContext db,
        ITenantContext tenant,
        ILogger<CustomerService> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    public async Task<IEnumerable<CustomerResponse>> GetAllAsync()
    {
        var customers = await _db.Customers
            .Include(c => c.Address)
            .Where(c => c.TenantId == _tenant.TenantId
                     && c.Status != CustomerStatus.Blocked)
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return customers.Select(MapToResponse);
    }

    public async Task<CustomerResponse?> GetByIdAsync(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Address)
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.TenantId == _tenant.TenantId);

        return customer is null ? null : MapToResponse(customer);
    }

    public async Task<CustomerResponse?> GetByEmailAsync(string email)
    {
        var customer = await _db.Customers
            .Include(c => c.Address)
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Email == email.ToLowerInvariant() &&
                c.TenantId == _tenant.TenantId);

        return customer is null ? null : MapToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        // Check for duplicate email within this tenant
        var exists = await _db.Customers.AnyAsync(c =>
            c.TenantId == _tenant.TenantId &&
            c.Email == request.Email.ToLowerInvariant());

        if (exists)
            throw new InvalidOperationException(
                $"Customer with email '{request.Email}' already exists");

        var customer = new Customer
        {
            TenantId = _tenant.TenantId,
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(),
            Phone = request.Phone,
            GstNumber = request.GstNumber?.ToUpperInvariant(),
            PanNumber = request.PanNumber?.ToUpperInvariant(),
            Status = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (request.Address is not null)
        {
            customer.Address = new CustomerAddress
            {
                Line1 = request.Address.Line1,
                Line2 = request.Address.Line2,
                City = request.Address.City,
                State = request.Address.State,
                PinCode = request.Address.PinCode,
                Country = request.Address.Country
            };
        }

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Tenant [{TenantId}] created customer {Name} <{Email}>",
            _tenant.TenantId, customer.Name, customer.Email);

        return MapToResponse(customer);
    }

    public async Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var customer = await _db.Customers
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.TenantId == _tenant.TenantId);

        if (customer is null) return null;

        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.GstNumber = request.GstNumber?.ToUpperInvariant();
        customer.PanNumber = request.PanNumber?.ToUpperInvariant();
        customer.UpdatedAt = DateTime.UtcNow;

        if (request.Address is not null)
        {
            if (customer.Address is null)
            {
                customer.Address = new CustomerAddress { CustomerId = customer.Id };
            }

            customer.Address.Line1 = request.Address.Line1;
            customer.Address.Line2 = request.Address.Line2;
            customer.Address.City = request.Address.City;
            customer.Address.State = request.Address.State;
            customer.Address.PinCode = request.Address.PinCode;
            customer.Address.Country = request.Address.Country;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Tenant [{TenantId}] updated customer {Id}",
            _tenant.TenantId, id);

        return MapToResponse(customer);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.TenantId == _tenant.TenantId);

        if (customer is null) return false;

        customer.Status = CustomerStatus.Inactive;
        customer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Tenant [{TenantId}] deactivated customer {Id}",
            _tenant.TenantId, id);

        return true;
    }

    // ── Mapper ────────────────────────────────────────────────────────────

    private static CustomerResponse MapToResponse(Customer c) => new(
        c.Id,
        c.Name,
        c.Email,
        c.Phone,
        c.GstNumber,
        c.PanNumber,
        c.Status.ToString(),
        c.CreatedAt,
        c.Address is null ? null : new AddressResponse(
            c.Address.Line1,
            c.Address.Line2,
            c.Address.City,
            c.Address.State,
            c.Address.PinCode,
            c.Address.Country
        )
    );
}