using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Models;
using TUDCoreService2._0.Utilities;
using TUDCoreService2._0.Camera;

namespace TUDCoreService2._0.Scale_Reader
{
    public interface IHandleScaleReader
    {
        public Task ProcessCommandHandler(TudCommand command, string workStationName, long workStationId, bool triggerUpdateCamera, bool delayResponse, long scaleId);

        public Task<RemoteScaleResponse> ProcessSignalRCommandHandler(TudCommand command, bool fireCamerOnRestart, bool delayResponse);

        public Task<string> GetTcpScaleWeight(TudCommand command, string workStationName, long workStationId, bool delayResponse);
        public void CloseConnections();
    }
}
