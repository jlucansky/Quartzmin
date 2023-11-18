using System.Threading.Tasks;
using HandlebarsDotNet;
using Microsoft.AspNetCore.SignalR;
using Quartzmin.Helpers;
using Quartzmin.Models;

namespace Quartzmin.Hubs
{
    public class QuartzHub: Hub
    {
        protected Services Services => (Services) Context.GetHttpContext()?.Items[typeof(Services)];

        public async Task GetScheduleInfoAsync()
        {
            var scheduleInfo = await new ScheduleInfoHelper().GetScheduleInfo(Services.Scheduler);

            await Clients.All.SendAsync("Update", scheduleInfo);
        }

        public async Task UpdateHistoryAsync()
        {
            // TODO: read from partial view file
            //Handlebars.Compile()
        }
    }
}