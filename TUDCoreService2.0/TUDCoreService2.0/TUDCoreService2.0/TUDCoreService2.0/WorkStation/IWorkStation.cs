using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.WorkStation
{
    public interface IWorkStation
    {
        [JsonProperty(PropertyName = "id")]
        public long id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; set; }

        [JsonProperty(PropertyName = "ip")]
        public string ip { get; set; }

        [JsonProperty(PropertyName = "port")]
        public string port { get; set; }

        [JsonProperty(PropertyName = "yardid")]
        public string yardid { get; set; }

        [JsonProperty(PropertyName = "command")]
        public string command { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public string created_at { get; set; }

        [JsonProperty(PropertyName = "updated_at")]
        public string updated_at { get; set; }



        IWorkStation GetConfiguredWorkstation(string ipAddress, string port);
        List<IWorkStation> GetWorkstations();
    }
}
