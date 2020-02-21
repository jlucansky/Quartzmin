using System;
using Quartzmin;
using System.Threading;

namespace NetSelfHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var scheduler = DemoScheduler.Create().Result;
            scheduler.Start();

            while (!scheduler.IsShutdown)
            {
                Console.WriteLine("Working");
                Thread.Sleep(500);
            }
        }
    }
}
