#if NETSTANDARD
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Quartzmin.Security
{
    public class UserAuthorizationFilter : IAuthorizationFilter
    {
        public UserPermissions[] RequiredUserPermissions { get; }

        public UserAuthorizationFilter(params UserPermissions[] requiredUserPermissions)
        {
            RequiredUserPermissions = requiredUserPermissions ?? new UserPermissions[0];
        }
        
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.HttpContext.DoesUserHavePermissions(RequiredUserPermissions))
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
#endif
