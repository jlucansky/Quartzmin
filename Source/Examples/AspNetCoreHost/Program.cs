﻿using AspNetCoreHost;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Quartzmin.AspNetCore
{
    class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().Build();
        }
    }
}
