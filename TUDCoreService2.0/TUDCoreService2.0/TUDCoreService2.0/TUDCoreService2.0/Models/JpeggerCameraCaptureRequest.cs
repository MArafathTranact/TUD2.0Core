using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Models
{
    public class JpeggerCameraCaptureRequest
    {
        public string IpAddress { get; set; }
        public int IpPort { get; set; }
        public string TicketNumber { get; set; }
        public string TransactionType { get; set; }
        public string TicketType { get; set; }
        public string CameraName { get; set; }
        public string Location { get; set; }
        public string EventCode { get; set; }
        public string TareSequenceNumber { get; set; }
        public string CustomerName { get; set; }
        public string CommodityName { get; set; }
        public string CommodityNumber { get; set; }
        public string Weight { get; set; }
        public int IsManual { get; set; }
        public string Amount { get; set; }
        public long ReceiptNumber { get; set; }
        public string CertificationNumber { get; set; }
        public string CertificationDate { get; set; }
        public string CertificateDescription { get; set; }
        public string CustomerNumber { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string Company { get; set; }
        public string SpecifyJpeggerTable { get; set; }
        public string CameraIpAddress { get; set; }
        public bool HasFileName { get; set; }
        public string FileName { get; set; }
        public string BookingNumber { get; set; }
        public string ContainerNumber { get; set; }
        public string ContractNumber { get; set; }
        public string ContractName { get; set; }
        public Guid ReferenceId { get; set; }
        public int ReferenceType { get; set; }
        public string GuidId { get; set; }
        public bool? SingleFileDelete { get; set; }
        public string YardId { get; set; }
        public string LiveCaptureCamera { get; set; }
        public string TableName { get; set; }
        public string BranchCode { get; set; }
        public string Initials { get; set; }
        public string AppDateTime { get; set; }
        public string Contract_Number { get; set; }
        public string RoutingNumber { get; set; }
        public string AccountNumber { get; set; }
        public string CheckNumber { get; set; }
        public string IdNumber { get; set; }
        public string ContractId { get; set; }

        public JpeggerCameraCaptureDataModel CaptureDataApi { get; set; }
        public bool IsScan { get; set; }

        public int JpeggerTry { get; set; }
    }
}
