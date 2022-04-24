#if NETSTANDARD

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Reflection;
using Quartzmin.Hubs;

namespace Quartzmin
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseQuartzmin(this IApplicationBuilder app, QuartzminOptions options, Action<Services> configure = null)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));

            app.UseFileServer(options);

            var services = Services.Create(options);
            configure?.Invoke(services);

            app.Use(async (context, next) =>
            {
                context.Items[typeof(Services)] = services;
                await next.Invoke();
            });
            
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var ex = context.Features.Get<IExceptionHandlerFeature>().Error;
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(services.ViewEngine.ErrorPage(ex));
                });
            });

            app.UseRouting();

            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute(
                    name: nameof(Quartzmin),
                    pattern: "{controller=Scheduler}/{action=Index}");
                
                routes.MapHub<QuartzHub>("quartzHub");
            });
        }

        private static void UseFileServer(this IApplicationBuilder app, QuartzminOptions options)
        {
            IFileProvider fs;
            if (string.IsNullOrEmpty(options.ContentRootDirectory))
                fs = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "Content");
            else
                fs = new PhysicalFileProvider(options.ContentRootDirectory);

            var fsOptions = new FileServerOptions()
            {
                RequestPath = new PathString("/Content"),
                EnableDefaultFiles = false,
                EnableDirectoryBrowsing = false,
                FileProvider = fs
            };

            app.UseFileServer(fsOptions);
        }

        public static void AddQuartzmin(this IServiceCollection services)
        {
            services.AddSignalR();
            
            services.AddMvc()
                .AddApplicationPart(Assembly.GetExecutingAssembly())
                .AddNewtonsoftJson();

        }

    }
}

#endif
