﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Utilities.Interface
{
    public interface ITudCommand
    {
        public string ticket_nbr { get; set; }
        public string receipt_nbr { get; set; }
        public string camera_name { get; set; }
        public string location { get; set; }
        public string event_code { get; set; }
        public decimal amount { get; set; }
        public string transaction_type { get; set; }
        public string branch_code { get; set; }
        public string commodity { get; set; }
        public string yardid { get; set; }
        public int tare_seq_nbr { get; set; }
        public string scaleName { get; set; }
        public string cameraName { get; set; }
        public string camera_group_id { get; set; }
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

        public TicketInformation ticket { get; set; }
    }
}
