#if NETSTANDARD
using Microsoft.AspNetCore.Mvc;

namespace Quartzmin.Security
{
    public class AuthorizeUserAttribute : TypeFilterAttribute
    {
        public AuthorizeUserAttribute(params UserPermissions[] requiredUserPermissions) : base(typeof(UserAuthorizationFilter))
        {
            Arguments = new object[] { requiredUserPermissions };
        }
    }
}
#endif
#if NETFRAMEWORK
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Quartzmin.Security
{
    public class AuthorizeUserAttribute : AuthorizeAttribute
    {
        public UserPermissions[] RequiredUserPermissions { get; }

        public AuthorizeUserAttribute(params UserPermissions[] requiredUserPermissions)
        {
            RequiredUserPermissions = requiredUserPermissions ?? throw new ArgumentNullException(nameof(requiredUserPermissions));
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            if (!actionContext.Request.GetOwinContext().DoesUserHavePermissions(RequiredUserPermissions))
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        }
    }
}
#endif