using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Camera
{
    public interface ICameraGroup
    {
        string cam_group { get; set; }
        string cam_name { get; set; }
        string yardid { get; set; }

        Task GetCameraGroups();

        Task<List<ICamera>> GetConfiguredCameraGroups(string cameraName);
    }
}
