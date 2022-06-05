using System;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Quartzmin.Hubs;

namespace Quartzmin
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseQuartzmin(this IApplicationBuilder app,
            QuartzminOptions options,
            Action<Services> configure = null)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));

            app.UseFileServer(options);

            var services = Services.Create(options);
            configure?.Invoke(services);

            // middleware
            app.Use(async (context, next) =>
            {
                context.Items[typeof(Services)] = services;
                await next.Invoke();
            });

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(services.ViewEngine.ErrorPage(ex));
                });
            });

            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute(
                    name: nameof(Quartzmin),
                    pattern: $"{options.VirtualPathRoot}/{{controller=Scheduler}}/{{action=Index}}");

                routes.MapHub<QuartzHub>($"{options.VirtualPathRoot}/quartzHub");
            });
        }

        private static void UseFileServer(this IApplicationBuilder app, QuartzminOptions options)
        {
            IFileProvider fs;
            if (string.IsNullOrEmpty(options.ContentRootDirectory))
            {
                fs = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "Content");
            }
            else
            {
                fs = new PhysicalFileProvider(options.ContentRootDirectory);
            }

            var fsOptions = new FileServerOptions
            {
                RequestPath = new PathString($"{options.VirtualPathRoot}/Content"),
                EnableDefaultFiles = false,
                EnableDirectoryBrowsing = false,
                FileProvider = fs
            };

            app.UseFileServer(fsOptions);
        }

        public static void AddQuartzmin(this IServiceCollection services, string virtualPathRoot = "")
        {
            services.AddSignalR();

            services.AddAuthentication(opt =>
                {
                    opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opt.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    opt.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.IsEssential = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.Name = "s-s-h";
                    options.LoginPath = $"{virtualPathRoot}/auth/login";
                    options.LogoutPath = $"{virtualPathRoot}/auth/logout";
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);
                });

            services.AddMvc()
                .AddApplicationPart(Assembly.GetExecutingAssembly())
                .AddNewtonsoftJson();
        }
    }
}
