using BillFlow.IdentityService.Models;

namespace BillFlow.IdentityService.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}