using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.SignalR
{
    public class GraphQLResponseData
    {
        public LocalComputerScales localComputerScales { get; set; }
    }

    public class LocalComputerScales
    {
        public List<LocalComputerScaleNode> nodes { get; set; }
    }

    public class LocalComputerScaleNode
    {
        public string id { get; set; }

        public string scaleName { get; set; }
        public string cameraName { get; set; }

        public int comPort { get; set; }
        public int baudRate { get; set; }
        public string dataStop { get; set; }
        public int? scaleParity { get; set; }
        public int bufferSize { get; set; }
        public int? weightBeginPosition { get; set; }
        public int? weightEndPosition { get; set; }
        public int? motionPosition { get; set; }
        public int? unitsPosition { get; set; }
        public int? modePosition { get; set; }
        public int? startOfText { get; set; }
        public int? noMotionChar { get; set; }
        public int? lbUnitsChar { get; set; }
        public int? grossModeChar { get; set; }
        public int? maxCharToRead { get; set; }
        public int? numberOfMatchingRead { get; set; }
        public bool useIpAddress { get; set; }
        public string ipAddress { get; set; }
        public int? ipPort { get; set; }

        public bool isBeamMonitorEnabled { get; set; }
        public string beamMonitorEndpoint { get; set; }

        public DateTime? disabledUntilAfterDate { get; set; }
        public bool isReturnWeightEnabled { get; set; }
        public int returnWeight { get; set; }

        public bool isStopLightEnabled { get; set; }
        public bool openClose { get; set; }

        public string stopLightEndpoint { get; set; }

        public bool isFireCameraEnabled { get; set; } = true;
        public bool isScaleSettingsUpdated { get; set; } = true;
    }
}
