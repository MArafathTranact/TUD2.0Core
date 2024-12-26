using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.Utilities
{
    public class TUDSettings : ITUDSettings
    {
        public string WorkStationIp { get; set; }
        public string WorkStationPort { get; set; }
        public string WorkStationWebSocket { get; set; }
        public string JPEGgerAPI { get; set; }
        public string JPEGgerToken { get; set; }
        public int IncludeToken { get; set; }
        public string ExecutablePath { get; set; }
        public string YardId { get; set; }
        public int TcpPort { get; set; }
        public int EnableTcpPort { get; set; }


    }
}
