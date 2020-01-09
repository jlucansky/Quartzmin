using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartzmin;

namespace AspNetCore3
{
	public class Program
	{
		public static void Main( string[] args )
		{
			var scheduler = DemoScheduler.Create().Result;

			var host = WebHost.CreateDefaultBuilder( args ).Configure( app =>
				{
					app.UseQuartzmin( new QuartzminOptions() { Scheduler = scheduler } );

				} ).ConfigureServices( services =>
				{
					services.AddQuartzmin();

				} )
				.Build();

			host.Start();

			while ( !scheduler.IsShutdown )
				Thread.Sleep( 250 );
		}
	}
}
