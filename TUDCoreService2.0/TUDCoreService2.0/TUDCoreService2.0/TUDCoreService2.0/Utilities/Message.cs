using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.Utilities
{
    public class Message
    {
        public int id { get; set; }
        public string ip { get; set; } = string.Empty;
        public string port { get; set; } = string.Empty;
        public string command { get; set; } = string.Empty;
    }
}
