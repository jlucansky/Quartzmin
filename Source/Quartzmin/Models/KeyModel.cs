using Quartz;

namespace Quartzmin.Models
{
    public class KeyModel
    {
        public string Name { get; set; }

        public string Group { get; set; }

        public JobKey ToJobKey() => new JobKey(Name, Group);

        public TriggerKey ToTriggerKey() => new TriggerKey(Name, Group);
    }
}
