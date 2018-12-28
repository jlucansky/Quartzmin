using Quartz;
using Quartz.Impl.Matchers;
using System.Collections.Generic;
using System.Linq;

namespace Quartzmin
{
    internal class Cache
    {
        private readonly Services _services;
        public Cache(Services services)
        {
            _services = services;
        }

        private string[] _jobTypes;
        public string[] JobTypes
        {
            get
            {
                if (_jobTypes == null)
                {
                    lock (this)
                    {
                        if (_jobTypes == null)
                        {
                            var keys = _services.Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()).GetAwaiter().GetResult();
                            var knownTypes = new List<string>();
                            foreach (var key in keys)
                            {
                                var detail = _services.Scheduler.GetJobDetail(key).GetAwaiter().GetResult();
                                knownTypes.Add(detail.JobType.RemoveAssemblyDetails());
                            }
                            UpdateJobTypes(knownTypes);
                        }
                    }
                }
                return _jobTypes;
            }
        }

        public void UpdateJobTypes(IEnumerable<string> list)
        {
            if (_jobTypes != null)
                list = list.Concat(_jobTypes); // append existing types
            _jobTypes = list.Distinct().OrderBy(x => x).ToArray();
        }

    }
}
