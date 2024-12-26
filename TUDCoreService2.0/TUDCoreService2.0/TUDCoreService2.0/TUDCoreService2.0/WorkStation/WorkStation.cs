using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.WorkStation
{
    public class WorkStation : IWorkStation
    {
        public List<IWorkStation> workStations;
        public int id { get; set; }
        public string name { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public string yardid { get; set; }
        public string command { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }

        private readonly IAPI _aPI;
        private readonly INLogger _logger;

        public WorkStation()
        {

        }
        public WorkStation(IAPI aPI, INLogger logger)
        {

            _aPI = aPI;
            _logger = logger;
            workStations = GetWorkstations();
        }

        public List<IWorkStation> GetWorkstations()
        {
            try
            {
                var workstations = _aPI.GetRequest<List<WorkStation>>("workstations");
                return workstations != null ? new List<IWorkStation>(workstations.Result.Cast<IWorkStation>()) : new List<IWorkStation>();

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Exception at Devices.GetWorkstations.", ex);
                return new List<IWorkStation>();
            }
        }

        public IWorkStation GetConfiguredWorkstation(string ipAddress, string port)
        {

            var workstation = workStations.Where(x => x.ip == ipAddress && x.port == port).FirstOrDefault();
            return workstation;
        }
    }
}
