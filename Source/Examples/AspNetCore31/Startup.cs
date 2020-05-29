using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartzmin;

namespace AspNetCore31
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddQuartzmin();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            var scheduler = DemoScheduler.Create().Result;
        
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseQuartzmin(new QuartzminOptions() { Scheduler = scheduler, VirtualPathRoot = "/Quartzmin" , UseLocalTime =true, DefaultDateFormat="yyyy-MM-dd", DefaultTimeFormat="HH:mm:ss" });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
     
        }
    }
}
