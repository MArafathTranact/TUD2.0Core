using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Camera
{
    public interface ICamera
    {
        [JsonProperty(PropertyName = "camera_name")]
        string camera_name { get; set; }

        [JsonProperty(PropertyName = "device_name")]
        string device_name { get; set; }

        [JsonProperty(PropertyName = "IsNetCam")]
        int IsNetCam { get; set; }

        [JsonProperty(PropertyName = "camera_type")]
        int camera_type { get; set; }

        [JsonProperty(PropertyName = "ip_address")]
        string ip_address { get; set; }

        [JsonProperty(PropertyName = "port_nbr")]
        int? port_nbr { get; set; }

        [JsonProperty(PropertyName = "username")]
        string username { get; set; }
        [JsonProperty(PropertyName = "pwd")]
        string pwd { get; set; }

        [JsonProperty(PropertyName = "yardid")]
        string yardid { get; set; }

        [JsonProperty(PropertyName = "URL")]
        string URL { get; set; }

        [JsonProperty(PropertyName = "videoURL")]
        string videoURL { get; set; }

        //[JsonProperty(PropertyName = "contract_id")]
        //int? contract_id { get; set; }

        //[JsonProperty(PropertyName = "contract_text")]
        //string contract_text { get; set; }

        //[JsonProperty(PropertyName = "isBasic")]
        //bool isBasic { get; set; }

        [JsonProperty(PropertyName = "workstation_ip")]
        string workstation_ip { get; set; }

        [JsonProperty(PropertyName = "workstation_port")]
        string workstation_port { get; set; }

        //string scale_camera_name { get; set; }
        //int? BaudRate { get; set; }
        //string DataStop { get; set; }
        //int? ScaleParity { get; set; }
        //int? ComPort { get; set; }
        //int? BufferSize { get; set; }
        //int? WeightBeginPosition { get; set; }
        //int? WeightEndPosition { get; set; }
        //int? MotionPosition { get; set; }
        //int? UnitsPosition { get; set; }
        //int? ModePosition { get; set; }
        //int? StartOfText { get; set; }
        //int? NoMotionChar { get; set; }
        //int? LbUnitsChar { get; set; }
        //int? GrossModeChar { get; set; }
        //int? MaxCharToRead { get; set; }
        //int? NumberOfMatchingRead { get; set; }

        List<ICamera> Cameras { get; set; }
        Task GetCameras();

        Task<ICamera> GetConfiguredCamera(string cameraName);

    }
}
