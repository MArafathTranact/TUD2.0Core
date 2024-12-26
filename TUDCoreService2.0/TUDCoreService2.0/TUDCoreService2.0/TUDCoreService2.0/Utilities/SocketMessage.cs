using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.Utilities
{
    public class SocketMessage
    {
        public string identifier { get; set; }
        public string type { get; set; }
        public Message message { get; set; }
    }
}
