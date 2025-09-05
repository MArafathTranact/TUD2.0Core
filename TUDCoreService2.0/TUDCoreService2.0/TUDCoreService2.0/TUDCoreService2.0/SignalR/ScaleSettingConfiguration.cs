using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.SignalR
{
    [Serializable]
    public class ScaleSettingConfiguration
    {
        public string EndPoint { get; set; }
        public string EncryptedToken { get; set; }

        public List<string> SettingsId { get; set; }
    }
}
