using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Utilities.Interface
{
    public interface INLogger
    {
        public void LogWithNoLock(string message);
        public void LogExceptionWithNoLock(string message, Exception exception);
        public void LogWarningWithNoLock(string message);
    }
}
