using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Models;
using TUDCoreService2._0.Utilities;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.Camera
{
    public class CameraGroup : ICameraGroup
    {
        #region Properties
        public string cam_group { get; set; }
        public string cam_name { get; set; }
        public string yardid { get; set; }

        public List<ICameraGroup> CameraGroups { get; set; }
        private IAPI _aPI;

        private readonly ITUDSettings _tudSettings;
        private readonly IConfiguration _configuration;
        private INLogger _logger;
        private readonly ICamera _camera;
        #endregion

        public CameraGroup()
        {

        }

        public CameraGroup(INLogger logger, IAPI aPI, ITUDSettings tUDSettings, IConfiguration configuration, ICamera camera)
        {
            _aPI = aPI;
            _logger = logger;
            _tudSettings = tUDSettings;
            _configuration = configuration;
            _camera = camera;
            _tudSettings = _configuration.GetSection("TUDSettings").Get<TUDSettings>();
            Task.Run(async () => await GetCameraGroups());
        }
        public async Task GetCameraGroups()
        {
            try
            {
                var cameras = await _aPI.GetRequest<List<CameraGroup>>($"camera_groups?yardid={_tudSettings.YardId}");
                if (cameras != null)
                    CameraGroups = new List<ICameraGroup>(cameras.Cast<ICameraGroup>());
                else
                    CameraGroups = new List<ICameraGroup>();

                if (CameraGroups != null && CameraGroups.Any())
                    _logger.LogWithNoLock($" {CameraGroups.Count} Camera groups loaded from Yard '{_tudSettings.YardId}'");
                else
                    _logger.LogWithNoLock($" 0 Camera groups loaded from Yard '{_tudSettings.YardId}'");


            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Exception at CameraGroup.GetCameraGroups.", ex);
            }
        }

        public async Task<List<ICamera>> GetConfiguredCameraGroups(string cameraGroupName)
        {
            if (CameraGroups?.Count > 0)
            {
                // var camGroup = CameraGroups.Where(x => x.cam_group == cameraGroupName).ToList();
                var camGroup = CameraGroups.Where(x => x.cam_group.ToLower() == cameraGroupName.ToLower() && !string.IsNullOrEmpty(x.cam_name));

                if (camGroup.Any())
                {
                    var cameras = _camera.Cameras;
                    return cameras.Where(x => camGroup.Any(z => x.camera_name.ToLower() == z.cam_name.ToLower())).ToList();
                }
            }
            return default;



        }
    }
}
