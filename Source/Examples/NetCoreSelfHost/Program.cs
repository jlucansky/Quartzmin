using Quartzmin;
using System.Threading;

namespace NetCoreSelfHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var scheduler = DemoScheduler.Create().Result;
            scheduler.Start();

            while (!scheduler.IsShutdown)
                Thread.Sleep(500);
        }
    }
}
