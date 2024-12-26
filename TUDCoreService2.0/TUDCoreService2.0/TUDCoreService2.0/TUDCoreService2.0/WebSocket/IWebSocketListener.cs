using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.WebSocket
{
    public interface IWebSocketListener
    {
        public Task ConnectWebSocket();
        public Task CloseWebSocket();

        public Task StartTcpListener();

        public Task CloseTcpListener();

    }
}
