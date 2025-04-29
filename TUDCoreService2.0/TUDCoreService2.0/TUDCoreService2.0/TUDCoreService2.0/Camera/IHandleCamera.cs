using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Models;

namespace TUDCoreService2._0.Camera
{
    public interface IHandleCamera
    {
        public Task TriggerCamera(JpeggerCameraCaptureRequest request, string workStationName, long workStationId);
    }
}
