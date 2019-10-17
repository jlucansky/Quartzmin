using System.Security.Claims;

namespace Quartzmin.Security
{
    public interface IAuthorizationProvider
    {
        UserPermissions[] GetUserPermissions(ClaimsPrincipal claimsPrincipal);
    }
}