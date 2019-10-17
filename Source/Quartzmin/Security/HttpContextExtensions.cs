#if NETSTANDARD
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Quartzmin.Security
{
    internal static class HttpContextExtensions
    {
        internal static bool DoesUserHavePermissions(this HttpContext httpContext, params UserPermissions[] userPermissions)
        {
            var items = httpContext.Items;
            var itemKey = typeof(Services);

            if (items.ContainsKey(itemKey))
            {
                if (items[itemKey] is Services services && services.AuthorizationProvider != null)
                {
                    var currentUserPermissions = services.AuthorizationProvider.GetUserPermissions(httpContext.User);
                    return userPermissions?.All(p => currentUserPermissions.Contains(p)) ?? true;
                }
            }

            return true;
        }

        internal static UserPermissions[] GetUserPermissions(this HttpContext httpContext)
        {
            var items = httpContext.Items;
            var itemKey = typeof(Services);

            if (items.ContainsKey(itemKey))
            {
                if (items[itemKey] is Services services && services.AuthorizationProvider != null)
                {
                    return services.AuthorizationProvider.GetUserPermissions(httpContext.User);
                }
            }

            return null;
        }
    }
}
#endif