using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.SignalR
{
    internal class APIConnection : IAPIConnection
    {
        private readonly INLogger _logger;

        private string _endPoint;

        private string _token;

        private List<string> _settingsId = [];

        public APIConnection(
            INLogger logger
            )
        {
            _logger = logger;
            ReadConfigFile();
        }

        public string GetEndPoint()
        {
            return _endPoint;
        }

        public string GetToken()
        {
            return _token;
        }

        public List<string> GetSettingsId()
        {
            return _settingsId;
        }
        public void ReadConfigFile()
        {
            try
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string pathname = Path.GetDirectoryName(path);

                string fileName = Path.Combine(pathname, "ScaleInfo.config");

                if (File.Exists(fileName))
                {

                    XmlSerializer serializer = new XmlSerializer(typeof(ScaleSettingConfiguration));

                    using (TextReader reader = new StreamReader(fileName))
                    {
                        ScaleSettingConfiguration configInfo =
                            (ScaleSettingConfiguration)serializer.Deserialize(reader);

                        if (configInfo != null)
                        {
                            _endPoint = configInfo.EndPoint;
                            _token = configInfo.EncryptedToken;
                            _settingsId = configInfo.SettingsId;

                        }
                        else
                            LogEvents($"No information found in config file located at '{fileName}'.");
                    }
                }
                else
                    LogEvents($"No config file found. Location ='{fileName}'");

            }
            catch (Exception ex)
            {

                _logger.LogExceptionWithNoLock($" Exception at WebSocketListener.ReadConficFile.", ex); throw;
            }
        }

        private void LogEvents(string input)
        {
            _logger.LogWithNoLock($" {input}");
        }

    }
}
