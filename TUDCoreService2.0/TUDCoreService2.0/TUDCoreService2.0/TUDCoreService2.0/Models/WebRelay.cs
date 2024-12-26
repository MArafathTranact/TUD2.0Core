using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TUDCoreService2._0.Models
{
    //case is following xml document
    [XmlType("datavalues")]
    public class WebRelay
    {
        public int input1state { get; set; }
        public int input2state { get; set; }
        public int relay1state { get; set; }
        public int relay2state { get; set; }
        public decimal hightime1 { get; set; }
        public decimal hightime2 { get; set; }
        public int count1 { get; set; }
        public int count2 { get; set; }
        public decimal extvar0 { get; set; }
        public decimal extvar1 { get; set; }
        public decimal extvar2 { get; set; }
        public decimal extvar3 { get; set; }
        public decimal extvar4 { get; set; }
        public string serialNumber { get; set; }
        public string time { get; set; }
    }
}
