using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Utilities.Interface;
using ILogger = NLog.ILogger;

namespace TUDCoreService2._0.Utilities
{
    public class NLogger : INLogger
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        public NLogger()
        {
            //_logger = logger;
        }
        public void LogExceptionWithNoLock(string message, Exception exception)
        {
            LogError(message, exception);
        }

        public void LogWarningWithNoLock(string message)
        {
            LogWarning(message);
        }

        public void LogWithNoLock(string message)
        {
            LogInfo(message);
        }

        private void LogInfo(string information)
        {
            try
            {
                _logger.Info(information);
            }
            catch (Exception)
            {
            }
        }

        private void LogError(string message, Exception exception)
        {
            try
            {
                _logger.Error(exception, message);
            }
            catch (Exception)
            {
            }
        }

        private void LogWarning(string message)
        {
            try
            {
                _logger.Warn(message);
            }
            catch (Exception)
            {
            }
        }
    }
}
