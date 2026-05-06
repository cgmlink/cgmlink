using CgmLink.Data.Entities;

namespace CgmLink.Identity.Services;

public interface ITokenService
{
    string GenerateJwtToken(User user);
    RefreshToken GenerateRefreshToken(string ipAddress);
}