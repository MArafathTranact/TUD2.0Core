using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Utilities.Interface
{
    public interface ISocketMessage
    {
        public string identifier { get; set; }
        public string type { get; set; }
        public Message message { get; set; }
    }
}
