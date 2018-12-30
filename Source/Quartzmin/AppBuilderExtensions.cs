#if NETFRAMEWORK

using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using MultipartDataMediaFormatter;
using MultipartDataMediaFormatter.Infrastructure;
using Owin;
using Quartzmin.Owin;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Hosting;
using System.Web.Http.Results;
using System.Web.Management;

namespace Quartzmin
{
    public static class AppBuilderExtensions
    {
        public static void UseQuartzmin(this IAppBuilder app, QuartzminOptions options, Action<Services> configure = null)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));

            app.UseFileServer(options);

            var services = Services.Create(options);
            configure?.Invoke(services);

            app.Use((owin, next) =>
            {
                owin.Set(Services.ContextKey, services);
                return next();
            });

            HttpConfiguration config = new HttpConfiguration();

            config.Routes.MapHttpRoute(
                name: nameof(Quartzmin),
                routeTemplate: "{controller}/{action}",
                defaults: new { controller = "Scheduler", action = "Index" }
            );

            config.Formatters.Add(new FormMultipartEncodedMediaTypeFormatter(new MultipartFormatterSettings() { ValidateNonNullableMissedProperty = false, CultureInfo = CultureInfo.InvariantCulture }));

            config.Services.Replace(typeof(IHostBufferPolicySelector), new BufferPolicySelector());
            config.Services.Replace(typeof(IExceptionHandler), new ExceptionHandler((IExceptionHandler)config.Services.GetService(typeof(IExceptionHandler)), services.ViewEngine.ErrorPage, true));

            app.UseWebApi(config);
        }

        class ExceptionHandler : IExceptionHandler
        {
            readonly bool _prettyPage;
            readonly IExceptionHandler _underlying;
            readonly Func<Exception, string> _render;
            public ExceptionHandler(IExceptionHandler underlying, Func<Exception, string> render, bool prettyPage)
            {
                _underlying = underlying;
                _prettyPage = prettyPage;
                _render = render;
            }

            private class ContentResult : IHttpActionResult
            {
                public string Content { get; set; }
                public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) {
                        Content = new StringContent(Content, Encoding.UTF8, "text/html") });
            }

            public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
            {
                var ex = context.Exception;

                if (ex is AggregateException aex)
                    ex = aex.InnerException;

                var httpException = ex.GetBaseException() as HttpException;
                
                if (httpException?.WebEventCode == WebEventCodes.RuntimeErrorPostTooLarge)
                {
                    context.Result = new StatusCodeResult(System.Net.HttpStatusCode.RequestEntityTooLarge, context.Request);
                    return Task.FromResult(0);
                }

                if (_prettyPage)
                {
                    context.Result = new ContentResult() { Content = _render(ex) };
                    return Task.FromResult(0);
                }
                else
                {
                    return _underlying.HandleAsync(context, cancellationToken);
                }
            }
        }

        class BufferPolicySelector : System.Web.Http.Owin.OwinBufferPolicySelector, IHostBufferPolicySelector
        {
            bool IHostBufferPolicySelector.UseBufferedInputStream(object hostContext) => true;
        }

        private static void UseFileServer(this IAppBuilder app, QuartzminOptions options)
        {
            IFileSystem fs;
            if (string.IsNullOrEmpty(options.ContentRootDirectory))
                fs = new FixedEmbeddedResourceFileSystem(Assembly.GetExecutingAssembly(), nameof(Quartzmin) + ".Content");
            else
                fs = new PhysicalFileSystem(options.ContentRootDirectory);

            var fsOoptions = new FileServerOptions()
            {
                RequestPath = new PathString("/Content"),
                EnableDefaultFiles = false,
                FileSystem = fs
            };

            var mimeMap = ((FileExtensionContentTypeProvider)fsOoptions.StaticFileOptions.ContentTypeProvider).Mappings;
            if (!mimeMap.ContainsKey(".woff2"))
                mimeMap.Add(".woff2", "application/font-woff2");

            app.UseFileServer(fsOoptions);
        }
    }
}

#endif
