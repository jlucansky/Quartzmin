#if NETFRAMEWORK
using System.Linq;
using System.Security.Claims;
using Microsoft.Owin;

namespace Quartzmin.Security
{
    internal static class OwinContextExtensions
    {
        internal static bool DoesUserHavePermissions(this IOwinContext owinContext, params UserPermissions[] userPermissions)
        {
            var services = owinContext.Get<Services>(Services.ContextKey);
            if (services?.AuthorizationProvider != null)
            {
                if (owinContext.Request.User is ClaimsPrincipal claimsPrincipal)
                {
                    var currentUserPermissions = services.AuthorizationProvider.GetUserPermissions(claimsPrincipal);
                    return userPermissions?.All(p => currentUserPermissions.Contains(p)) ?? true;
                }
            }

            return true;
        }

        internal static UserPermissions[] GetUserPermissions(this IOwinContext owinContext)
        {
            var services = owinContext.Get<Services>(Services.ContextKey);
            if (services?.AuthorizationProvider != null)
            {
                if (owinContext.Request.User is ClaimsPrincipal claimsPrincipal)
                {
                    return services.AuthorizationProvider.GetUserPermissions(claimsPrincipal);
                }
            }

            return null;
        }
    }
}
#endif