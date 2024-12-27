using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Models;
using TUDCoreService2._0.Utilities;
using TUDCoreService2._0.Utilities.Interface;
using TUDCoreService2._0.WorkStation;

namespace TUDCoreService2._0.Camera
{
    public class Camera : ICamera
    {
        #region Properties
        public string camera_name { get; set; }

        public string device_name { get; set; }

        public int IsNetCam { get; set; }

        public int camera_type { get; set; }

        public string ip_address { get; set; }

        public int? port_nbr { get; set; }

        public string username { get; set; }
        public string pwd { get; set; }

        public string yardid { get; set; }

        public string URL { get; set; }

        public string videoURL { get; set; }

        public string workstation_ip { get; set; }

        public string workstation_port { get; set; }

        //[JsonProperty(PropertyName = "scale_camera_name")]
        //public string scale_camera_name { get; set; }

        //[JsonProperty(PropertyName = "BaudRate")]
        //public int? BaudRate { get; set; }
        //[JsonProperty(PropertyName = "DataStop")]
        //public string DataStop { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? ScaleParity { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? ComPort { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? BufferSize { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? WeightBeginPosition { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? WeightEndPosition { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? MotionPosition { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? UnitsPosition { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? ModePosition { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? StartOfText { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? NoMotionChar { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? LbUnitsChar { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? GrossModeChar { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? MaxCharToRead { get; set; }
        //[JsonProperty(PropertyName = "id")]
        //public int? NumberOfMatchingRead { get; set; }


        public List<ICamera> Cameras { get; set; }
        private IAPI _aPI;

        private readonly ITUDSettings _tudSettings;
        private readonly IConfiguration _configuration;
        private INLogger _logger;
        #endregion

        public Camera()
        {

        }
        public Camera(
            IAPI aPI,
            INLogger logger,
            ITUDSettings tudSettings,
            IConfiguration configuration)
        {
            _aPI = aPI;
            _logger = logger;
            _tudSettings = tudSettings;
            _configuration = configuration;
            _tudSettings = _configuration.GetSection("TUDSettings").Get<TUDSettings>();
            Task.Run(async () => await GetCameras());
        }

        public async Task GetCameras()
        {
            try
            {
                var cameras = await _aPI.GetRequest<List<Camera>>($"cameras?yardid={_tudSettings.YardId}");
                if (cameras != null)
                    Cameras = new List<ICamera>(cameras.Cast<ICamera>());
                else
                    Cameras = new List<ICamera>();

                if (Cameras != null && Cameras.Any())
                    _logger.LogWithNoLock($" {Cameras.Count} Cameras loaded from Yard '{_tudSettings.YardId}'");
                else
                    _logger.LogWithNoLock($" 0 Cameras loaded from Yard '{_tudSettings.YardId}'");

                StringBuilder sb = new StringBuilder();
                foreach (var camera in Cameras)
                {
                    if (!string.IsNullOrEmpty(camera.videoURL)
                       && !string.IsNullOrEmpty(camera.ip_address))
                    {
                        sb.Clear();
                        sb.Append("http://");
                        if (!string.IsNullOrEmpty(camera.username)
                            && !string.IsNullOrEmpty(camera.pwd))
                        {
                            sb.Append(camera.username);
                            sb.Append(":");
                            sb.Append(camera.pwd);
                            sb.Append("@");
                        }

                        sb.Append(camera.ip_address);
                        sb.Append(camera.videoURL);
                        camera.videoURL = sb.ToString();
                    }

                    if (!string.IsNullOrEmpty(camera.URL)
                        && !string.IsNullOrEmpty(camera.ip_address))
                    {
                        sb.Clear();
                        sb.Append("http://");
                        sb.Append(camera.ip_address);
                        sb.Append(camera.URL);
                        camera.URL = sb.ToString();
                    }

                }
                sb = null;

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Exception at Camera.GetCameras.", ex);
            }
        }

        public async Task<ICamera> GetConfiguredCamera(string cameraName)
        {
            var camera = Cameras.Where(x => x.camera_name == cameraName).FirstOrDefault();
            return camera;
        }
    }
}
