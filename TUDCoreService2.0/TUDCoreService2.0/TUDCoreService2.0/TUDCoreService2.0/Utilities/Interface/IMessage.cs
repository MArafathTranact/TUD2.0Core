using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Utilities.Interface
{
    public interface IMessage
    {
        public int id { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public string command { get; set; }
    }
}
