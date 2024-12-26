using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Utilities.Interface
{
    public interface ITUDSettings
    {
        string WorkStationIp { get; set; }
        string WorkStationPort { get; set; }
        string WorkStationWebSocket { get; set; }
        string JPEGgerAPI { get; set; }
        string JPEGgerToken { get; set; }
        int IncludeToken { get; set; }
        string ExecutablePath { get; set; }
        string YardId { get; set; }
        int TcpPort { get; set; }
        int EnableTcpPort { get; set; }
    }
}
