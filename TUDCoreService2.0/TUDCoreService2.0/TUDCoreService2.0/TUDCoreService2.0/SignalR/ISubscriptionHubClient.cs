using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUDCoreService2._0.SignalR
{
    public interface ISubscriptionHubClient
    {
        HubConnection _connection { get; }
        Task<bool> StartAsync(CancellationToken cancellationToken = default);
        Task<bool> CloseAsync(CancellationToken cancellationToken = default);
    }
}
