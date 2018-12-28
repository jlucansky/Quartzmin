using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Quartzmin.Models
{
    public class JobListItem
    {
        public string JobName { get; set; }

        public string Group { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }


        public bool Recovery { get; set; }

        public bool Persist { get; set; } // Persist job data

        public bool Concurrent { get; set; }

        public Histogram History { get; set; }
    }
}
