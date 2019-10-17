using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Quartzmin.Models;
using Quartz;
using Quartzmin.Security;

namespace Quartzmin.Controllers
{
    #region Target-Specific Directives

#if NETSTANDARD
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Primitives;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public abstract partial class PageControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new DefaultContractResolver(), // PascalCase as default
        };

        protected Services Services => (Services) Request.HttpContext.Items[typeof(Services)];
        protected string GetRouteData(string key) => RouteData.Values[key].ToString();
        protected IActionResult Json(object content) => new JsonResult(content, _serializerSettings);

        protected IActionResult NotModified() => new StatusCodeResult(304);

        protected IEnumerable<string> GetHeader(string key)
        {
            var values = Request.Headers[key];
            return values == StringValues.Empty ? (IEnumerable<string>)null : values;
        }

        protected bool UserHasPermissions(params UserPermissions[] userPermissions)
        {
            return HttpContext.DoesUserHavePermissions(userPermissions);
        }

        protected UserPermissions[] GetUserPermissions()
        {
            return HttpContext.GetUserPermissions();
        }
    }
#endif
#if NETFRAMEWORK
    using IActionResult = System.Web.Http.IHttpActionResult;
    using System.Net.Http;
    using System.Web.Http.Results;

    public abstract partial class PageControllerBase : System.Web.Http.ApiController
    {
        protected Services Services => Request.GetOwinContext().Get<Services>(Services.ContextKey);
        protected string GetRouteData(string key) => ControllerContext.RouteData.Values[key].ToString();
        protected IActionResult Json(object content) => base.Json(content);

        private class ContentResult : IActionResult
        {
            public string Content { get; set; }
            public string ContentType { get; set; }
            public DateTimeOffset? LastModified { get; set; }
            public string ETag { get; set; }

            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var msg = new HttpResponseMessage() {Content = new StringContent(Content)};
                msg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType);

                if (!string.IsNullOrEmpty(ETag))
                    msg.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(ETag);

                if (LastModified != null)
                    msg.Content.Headers.LastModified = LastModified;

                return Task.FromResult(msg);
            }
        }

        protected IActionResult NoContent() => new StatusCodeResult(System.Net.HttpStatusCode.NoContent, Request);
        protected IActionResult NotModified() => new StatusCodeResult(System.Net.HttpStatusCode.NotModified, Request);

        protected IEnumerable<string> GetHeader(string key)
        {
            if (Request.Headers.TryGetValues(key, out var values))
                return values;
            else
                return null;
        }

        protected bool UserHasPermissions(params UserPermissions[] userPermissions)
        {
            return Request.GetOwinContext().DoesUserHavePermissions(userPermissions);
        }

        protected UserPermissions[] GetUserPermissions()
        {
            return Request.GetOwinContext().GetUserPermissions();
        }
    }
#endif
    #endregion

    public abstract partial class PageControllerBase
    {
        protected IScheduler Scheduler => Services.Scheduler;

        protected IAuthorizationProvider AuthorizationProvider => Services.AuthorizationProvider;

        protected dynamic ViewBag { get; } = new ExpandoObject();

        internal class Page
        {
            PageControllerBase _controller;

            public string ControllerName => _controller.GetRouteData("controller");

            public string ActionName => _controller.GetRouteData("action");

            public Services Services => _controller.Services;

            public object ViewBag => _controller.ViewBag;

            public object Model { get; set; }

            public object UserPermissions { get; private set; }

            public Page(PageControllerBase controller, object model = null, UserPermissions[] userPermissions = null)
            {
                _controller = controller;
                Model = model;

                SetUserPermissions(userPermissions);
            }

            private void SetUserPermissions(UserPermissions[] userPermissions = null)
            {
                dynamic userPermissionsBag = new ExpandoObject();

                var properties = userPermissionsBag as IDictionary<String, object>;
                foreach (var userPermission in Enum.GetValues(typeof(UserPermissions)).Cast<UserPermissions>())
                {
                    properties["Can" + userPermission] = userPermissions?.Contains(userPermission) ?? true;
                }

                UserPermissions = userPermissionsBag;
            }
        }

        protected IActionResult View(object model)
        {
            return View(GetRouteData("action"), model);
        }

        protected IActionResult View(string viewName, object model)
        {
            string content = Services.ViewEngine.Render($"{GetRouteData("controller")}/{viewName}.hbs", new Page(this, model, GetUserPermissions()));
            return Html(content);
        }

        protected IActionResult Html(string html)
        {
            return new ContentResult()
            {
                Content = html,
                ContentType = "text/html",
            };
        }

        protected string GetETag()
        {
            IEnumerable<string> values = GetHeader("If-None-Match");
            if (values == null)
                return null;
            else
                return new System.Net.Http.Headers.EntityTagHeaderValue(values.FirstOrDefault()).Tag;
        }

        public IActionResult TextFile(string content, string contentType, DateTime lastModified, string etag)
        {
#if NETSTANDARD
            Response.Headers.Add("Last-Modified", lastModified.ToUniversalTime().ToString("R"));
            Response.Headers.Add("ETag", etag);
#endif
            return new ContentResult()
            {
                Content = content,
                ContentType = contentType,
#if NETFRAMEWORK
                ETag = etag,
                LastModified = lastModified
#endif
            };
        }

        protected JobDataMapItem JobDataMapItemTemplate => new JobDataMapItem()
        {
            SelectedType = Services.Options.DefaultSelectedType,
            SupportedTypes = Services.Options.StandardTypes.Order(),
        };
    }
}
