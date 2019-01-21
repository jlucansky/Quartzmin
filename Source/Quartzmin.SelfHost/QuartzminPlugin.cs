using Quartz;
using Quartz.Spi;
using System;
using System.Threading;
using System.Threading.Tasks;

#if NETSTANDARD
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
#endif

namespace Quartzmin.SelfHost
{
    public class QuartzminPlugin : ISchedulerPlugin
    {
        public string Url { get; set; }

        public string DefaultDateFormat { get; set; }
        public string DefaultTimeFormat { get; set; }

        public string Logo { get; set; }
        public string ProductName { get; set; }

        private IScheduler _scheduler;
        private IDisposable _webApp;

        public Task Initialize(string pluginName, IScheduler scheduler, CancellationToken cancellationToken = default(CancellationToken))
        {
            _scheduler = scheduler;
            return Task.FromResult(0);
        }

#if NETSTANDARD
        public async Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            var host = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder().Configure(app => {
                app.UseQuartzmin(CreateQuartzminOptions());
            }).ConfigureServices(services => {
                services.AddQuartzmin();
            })
            .ConfigureLogging(logging => {
                logging.ClearProviders();
            })
            .UseUrls(Url)
            .Build();

            _webApp = host;

            await host.StartAsync();
        }
#endif

#if NETFRAMEWORK
        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            _webApp = Microsoft.Owin.Hosting.WebApp.Start(Url, app => {
                app.UseQuartzmin(CreateQuartzminOptions());
            });
            return Task.FromResult(0);
        }
#endif
        public Task Shutdown(CancellationToken cancellationToken = default(CancellationToken))
        {
            _webApp.Dispose();
            return Task.FromResult(0);
        }

        private QuartzminOptions CreateQuartzminOptions()
        {
            var options = new QuartzminOptions()
            {
                Scheduler = _scheduler,
            };

            if (!string.IsNullOrEmpty(DefaultDateFormat))
                options.DefaultDateFormat = DefaultDateFormat;
            if (!string.IsNullOrEmpty(DefaultTimeFormat))
                options.DefaultTimeFormat = DefaultTimeFormat;
            if (!string.IsNullOrEmpty(Logo))
                options.Logo = Logo;
            if (!string.IsNullOrEmpty(ProductName))
                options.ProductName = ProductName;

            return options;
        }

    }
}
