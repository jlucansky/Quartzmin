using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Quartzmin;

namespace AspNetCoreHost;

public class Startup
{
    private const string _virtialPathRoot = "/quartzmin";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddQuartzmin(_virtialPathRoot);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Items["header"] = "a";
            await next.Invoke(context);
        });

        app.UseQuartzmin(new QuartzminOptions
        {
            Scheduler = DemoScheduler.Create().Result,
            VirtualPathRoot = _virtialPathRoot,
            DeployedAsWebAppliaction = false
        });

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", () => "Hello");
        });
    }
}