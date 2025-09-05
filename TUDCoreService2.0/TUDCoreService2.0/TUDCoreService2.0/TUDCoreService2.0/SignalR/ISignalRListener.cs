using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.SignalR
{
    public interface ISignalRListener
    {
        public Task ConnectSignalR();
        public Task CloseSignalR();

    }
}
