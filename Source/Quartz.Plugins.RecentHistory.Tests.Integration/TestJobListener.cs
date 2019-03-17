using System;
using System.Threading;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory.Tests.Integration
{
    public class TestJobListener : IJobListener
    {
        public static Action<IJobExecutionContext, JobExecutionException> JobWasExecutedCallback { get; set; }

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException,
            CancellationToken cancellationToken = new CancellationToken())
        {
            JobWasExecutedCallback(context, jobException);
            return Task.CompletedTask;
        }

        public string Name { get; set; } = nameof(TestJobListener);
    }
}
