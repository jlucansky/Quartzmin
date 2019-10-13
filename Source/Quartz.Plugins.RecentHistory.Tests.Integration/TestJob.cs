using System;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory.Tests.Integration
{
    public class TestJob : IJob
    {
        public static Action<IJobExecutionContext> Callback { get; set; }
        
        public Task Execute(IJobExecutionContext context)
        {
            if (Callback != null)
            {
                Callback(context);
            }

            return Task.CompletedTask;
        }
    }
}
