using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Quartzmin.AspNetCore
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddQuartzmin();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseQuartzmin(new QuartzminOptions()
            {
                Scheduler = DemoScheduler.Create().Result,
            });
        }
    }
}
