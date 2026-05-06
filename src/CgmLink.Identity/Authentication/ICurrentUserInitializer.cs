using System.Security.Claims;

namespace CgmLink.Identity.Authentication;

public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);
}