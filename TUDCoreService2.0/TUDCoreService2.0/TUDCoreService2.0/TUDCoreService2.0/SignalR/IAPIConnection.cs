using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.SignalR
{
    public interface IAPIConnection
    {
        void ReadConfigFile();
        string GetEndPoint();
        string GetToken();
        List<string> GetSettingsId();
    }
}
