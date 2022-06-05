using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Quartzmin;

namespace AspNetCoreHost;

public class Startup
{
    private readonly string _virtialPathRoot = "/q";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddQuartzmin(_virtialPathRoot);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", () => "Hello");
        });

        app.UseQuartzmin(new QuartzminOptions()
        {
            Scheduler = DemoScheduler.Create().Result,
            VirtualPathRoot = _virtialPathRoot
        });
    }
}