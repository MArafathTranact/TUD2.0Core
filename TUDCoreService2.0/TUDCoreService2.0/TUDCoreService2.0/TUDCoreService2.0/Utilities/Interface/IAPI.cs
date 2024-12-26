using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.Utilities.Interface
{
    public interface IAPI
    {
        public Task<T> PutRequest<T>(T updateItem, string param);
        public Task<T> GetRequest<T>(string param);
    }
}
