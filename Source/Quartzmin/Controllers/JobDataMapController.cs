using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartzmin.Helpers;
using Quartzmin.Models;
using Quartzmin.TypeHandlers;

namespace Quartzmin.Controllers
{
    public class JobDataMapController : PageControllerBase
    {
        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> ChangeTypeAsync()
        {
            var formData = await Request.GetFormDataAsync();

            TypeHandlerBase selectedType, targetType;
            try
            {
                selectedType = Services.TypeHandlers.Deserialize((string)formData.First(x => x.Key == "selected-type").Value);
                targetType = Services.TypeHandlers.Deserialize((string)formData.First(x => x.Key == "target-type").Value);
            }
            catch (JsonSerializationException ex) when (ex.Message.StartsWith("Could not create an instance of type"))
            {
                return new BadRequestResult { ReasonPhrase = "Unknown Type Handler" };
            }

            var dataMapForm = (await formData.GetJobDataMapFormAsync(includeRowIndex: false)).SingleOrDefault(); // expected single row

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

        private class BadRequestResult : IActionResult
        {
            public string ReasonPhrase { get; set; }
            public Task ExecuteResultAsync(ActionContext context)
            {
                context.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = ReasonPhrase;
                return Task.FromResult(0);
            }
        }

        [HttpGet, ActionName("TypeHandlers.js")]
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
