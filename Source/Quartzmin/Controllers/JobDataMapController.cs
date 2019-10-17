using Quartzmin.Helpers;
using Quartzmin.Models;
using Quartzmin.TypeHandlers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Quartzmin.Security;

#region Target-Specific Directives
#if NETSTANDARD
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
#endif
#if NETFRAMEWORK
using System.Web.Http;
using IActionResult = System.Web.Http.IHttpActionResult;
using System.Web.Http.Results;
#endif
#endregion

namespace Quartzmin.Controllers
{
    public class JobDataMapController : PageControllerBase
    {
        [HttpPost, JsonErrorResponse]
        [AuthorizeUser(UserPermissions.ViewJobs)]
        public async Task<IActionResult> ChangeType()
        {
            var formData = await Request.GetFormData();

            TypeHandlerBase selectedType, targetType;
            try
            {
                selectedType = Services.TypeHandlers.Deserialize((string)formData.First(x => x.Key == "selected-type").Value);
                targetType = Services.TypeHandlers.Deserialize((string)formData.First(x => x.Key == "target-type").Value);
            }
            catch (JsonSerializationException ex) when (ex.Message.StartsWith("Could not create an instance of type"))
            {
                return new BadRequestResult() { ReasonPhrase = "Unknown Type Handler" };
            }

            var dataMapForm = (await formData.GetJobDataMapForm(includeRowIndex: false)).SingleOrDefault(); // expected single row

            object oldValue = selectedType.ConvertFrom(dataMapForm);

            // phase 1: direct conversion
            object newValue = targetType.ConvertFrom(oldValue);

            if (oldValue != null && newValue == null) // if phase 1 failed
            {
                // phase 2: conversion using invariant string
                var str = selectedType.ConvertToString(oldValue);
                newValue = targetType.ConvertFrom(str);
            }

            return Html(targetType.RenderView(Services, newValue));
        }

#if NETSTANDARD
        private class BadRequestResult : IActionResult
        {
            public string ReasonPhrase { get; set; }
            public Task ExecuteResultAsync(ActionContext context)
            {
                context.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = ReasonPhrase;
                return Task.FromResult(0);
            }
        }
#endif
#if NETFRAMEWORK
        private class BadRequestResult : IActionResult
        {
            public string ReasonPhrase { get; set; }
            public Task<System.Net.Http.HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken) =>
                Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.BadRequest) { ReasonPhrase = ReasonPhrase });
        }
#endif

        [HttpGet, ActionName("TypeHandlers.js")]
        [AuthorizeUser(UserPermissions.ViewJobs)]
        public IActionResult TypeHandlersScript()
        {
            var etag = Services.TypeHandlers.LastModified.ETag();

            if (etag.Equals(GetETag()))
                return NotModified();

            StringBuilder execStubBuilder = new StringBuilder();
            execStubBuilder.AppendLine();
            foreach (var func in new[] { "init" })
                execStubBuilder.AppendLine(string.Format("if (f === '{0}' && {0} !== 'undefined') {{ {0}.call(this); }}", func));

            string execStub = execStubBuilder.ToString();

            var js = Services.TypeHandlers.GetScripts().ToDictionary(x => x.Key, 
                x => new JRaw("function(f) {" + x.Value + execStub + "}"));

            return TextFile("var $typeHandlerScripts = " + JsonConvert.SerializeObject(js) + ";", 
                "application/javascript", Services.TypeHandlers.LastModified, etag);
        }
    }
}
