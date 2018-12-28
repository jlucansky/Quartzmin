using Quartzmin.TypeHandlers;

namespace Quartzmin.Models
{
    public class JobDataMapItem : JobDataMapItemBase
    {
        public TypeHandlerBase[] SupportedTypes { get; set; }

        public string Description { get; set; }

        public bool Enabled { get; set; } = true;

        public bool EditableName { get; set; } = true;

        public bool Mandatory { get; set; }
    }
}
