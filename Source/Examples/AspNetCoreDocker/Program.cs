using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Quartzmin;
using System.Threading;

namespace AspNetCoreDocker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var scheduler = DemoScheduler.Create().Result;

            var host = WebHost.CreateDefaultBuilder(args).Configure(app => 
            {
                app.UseQuartzmin(new QuartzminOptions() { Scheduler = scheduler });

            }).ConfigureServices(services => 
            {
                services.AddQuartzmin();

            })
            .Build();

            host.Start();

            while (!scheduler.IsShutdown)
                Thread.Sleep(250);
        }

    }
}

/*
docker run -d -p 9999:80 --name myapp quartzmin
docker exec -it myapp sh

docker run -it --rm -p 9999:80 --name myapp quartzmin

docker tag quartzmin docker:5000/quartzmin
docker push docker:5000/quartzmin
*/
