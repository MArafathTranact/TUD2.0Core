using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Utilities
{
    public class Scale
    {
        public long id { get; set; }
        public string name { get; set; }
        public string command { get; set; }

        public long workstation_id { get; set; }
        public string ticket_number { get; set; }

        public string event_code { get; set; }
    }
}
