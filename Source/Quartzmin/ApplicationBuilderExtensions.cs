namespace Quartzmin;

public static class ApplicationBuilderExtensions
{
    public static void UseQuartzmin(this IApplicationBuilder app,
        QuartzminOptions options,
        Action<Services> configure = null)
    {
        var opt = app.ApplicationServices.GetService<QuartzminOptions>();

        options = options ?? throw new ArgumentNullException(nameof(options));

        // override the virtual path root from AddQuartzmin
        if (opt != null && opt.VirtualPathRoot != "/")
        {
            options.VirtualPathRoot = opt.VirtualPathRoot;
        }

        if (opt != null && !string.IsNullOrEmpty(opt.WebAppName))
        {
            options.WebAppName = opt.WebAppName;
        }

        app.UseFileServer(options);

        var services = Services.Create(options);
        configure?.Invoke(services);

        // middleware
        app.Use(async (context, next) =>
        {
            context.Items[typeof(Services)] = services;
            await next.Invoke(context);
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
                pattern: $"{options.VirtualPathRoot}{{controller=Scheduler}}/{{action=Index}}");

            routes.MapHub<QuartzHub>($"{options.VirtualPathRoot}quartzHub");
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
            RequestPath = new PathString($"{options.VirtualPathRoot}Content"),
            EnableDefaultFiles = false,
            EnableDirectoryBrowsing = false,
            FileProvider = fs
        };

        app.UseFileServer(fsOptions);
    }

    /// <summary>
    /// Add Quartzmin app
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="virtualPathRoot">Virtual path root, default empty; if set, it will replace the value set in UseQuartzmin</param>
    /// <param name="webAppName">Web Application Name of IIS WebSite</param>
    public static void AddQuartzmin(this IServiceCollection services, string virtualPathRoot = "", string webAppName = "")
    {
        var options = new QuartzminOptions
        {
            VirtualPathRoot = virtualPathRoot,
            WebAppName = webAppName
        };
        services.AddSingleton(options);

        services.AddSignalR();

        services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                opt.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opt =>
            {
                opt.Cookie.IsEssential = true;
                opt.Cookie.HttpOnly = true;
                opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                opt.Cookie.SameSite = SameSiteMode.Strict;
                opt.Cookie.Name = "s-s-h";
                opt.LoginPath = $"{options.VirtualPathRoot}auth/login";
                opt.LogoutPath = $"{options.VirtualPathRoot}auth/logout";
                opt.ExpireTimeSpan = TimeSpan.FromHours(1);
            });

        services.AddMvc()
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddNewtonsoftJson();
    }
}