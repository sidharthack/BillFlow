using BillFlow.TenantService.DTOs;
using BillFlow.TenantService.Models;

namespace BillFlow.TenantService.Services;

public interface ITenantService
{
    Task<TenantResponse?> GetBySlugAsync(string slug);
    Task<TenantResponse?> GetByIdAsync(int id);
    Task<IEnumerable<TenantResponse>> GetAllAsync();
    Task<TenantResponse> RegisterAsync(RegisterTenantRequest request);
    Task<bool> ExistsAsync(string slug);
}