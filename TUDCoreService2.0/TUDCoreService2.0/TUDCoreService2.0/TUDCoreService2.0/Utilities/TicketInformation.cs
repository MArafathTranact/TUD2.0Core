using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.Utilities
{
    public class TicketInformation : ITicketInformation
    {
        public string event_code { get; set; }
        public string ticket_nbr { get; set; }
        public string camera_name { get; set; }
        public string location { get; set; }
        public string transaction_type { get; set; }
        public string branch_code { get; set; }
        public string cust_nbr { get; set; }
    }
}
