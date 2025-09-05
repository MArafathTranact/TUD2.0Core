using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.SignalR
{
    public class OnPortalScaleReadRequestReceivedPayload
    {
        public Guid ScaleId { get; set; }
        public Guid YardId { get; set; }

        public string EventCode { get; set; }

        public string TicketNumber { get; set; }

        public string Commodity { get; set; }

        public int TareSeqNumber { get; set; }

        public decimal Amount { get; set; }

        public decimal ScaleValue { get; set; }

        public bool IsFireCameraEnabled { get; set; }
    }

    public class OnPortalScaleReadResponseReceivedPayload
    {
        public string Error { get; set; }
        public string Scale { get; set; }
        public Guid ScaleId { get; set; }
        public bool Status { get; set; }
    }
}
