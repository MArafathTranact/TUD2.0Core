using Microsoft.AspNetCore.SignalR.Client;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.SignalR
{
    public class SubscriptionHubClient : ISubscriptionHubClient
    {
        private readonly INLogger _logger;
        private readonly IAPIConnection _aPIConnection;
        public HubConnection _connection { get; }

        public SubscriptionHubClient(INLogger logger,
            IAPIConnection aPIConnection)
        {
            try
            {
                _logger = logger;
                _aPIConnection = aPIConnection;
                var endpoint = _aPIConnection.GetEndPoint();
                if (endpoint != null)
                {
                    _connection = new HubConnectionBuilder()
                   .WithUrl($"{endpoint}/subscriptions") // Website URL
                   .WithAutomaticReconnect()
                   .Build();

                    _connection.Reconnecting += error =>
                    {
                        LogEvents($"SubscriptionHubClient is reconnecting. State {_connection.State} ");
                        return Task.CompletedTask;
                    };

                    _connection.Reconnected += connectionId =>
                    {
                        LogEvents($"SubscriptionHubClient is connected - {connectionId}.State ={_connection.State}");
                        return Task.CompletedTask;
                    };

                    _connection.Closed += async error =>
                    {
                        LogEvents($"SubscriptionHubClient is disconnected. State={_connection.State}");
                        LogEvents($"SubscriptionHubClient is reconnecting... ");
                        await StartAsync();
                        LogEvents($"SubscriptionHubClient connected.");
                        // return Task.CompletedTask;
                    };
                }
                else
                    LogEvents($"No valid end point provided.");
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($"Exception in SubscriptionHubClient.", ex);
            }


        }

        private void SubscribeToSignalR()
        {

        }

        public Task<bool> CloseAsync(CancellationToken cancellationToken = default) => CloseSafely(cancellationToken);


        public Task<bool> StartAsync(CancellationToken cancellationToken = default) => ConnectWithRetryAsync(cancellationToken);


        private async Task<bool> CloseSafely(CancellationToken cancellationToken = default)
        {
            if (_connection == null) return true;

            try
            {
                LogEvents($"Stopping sunscription to the hub.");
                await _connection.StopAsync(cancellationToken);
                Debug.Assert(_connection.State == HubConnectionState.Disconnected);
                LogEvents($"Stopped sunscription to the hub.");
                return true;
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch
            {
                // Failed to close, trying again in 5000 ms.
                Debug.Assert(_connection.State != HubConnectionState.Disconnected);
                return false;
            }
        }

        private async Task<bool> ConnectWithRetryAsync(CancellationToken cancellationToken = default)
        {
            // Keep trying to until we can start or the cancellationToken is canceled.
            while (true)
            {
                try
                {
                    if (_connection == null) return true;
                    LogEvents($"Connecting with hub.");
                    await _connection.StartAsync(cancellationToken);
                    Debug.Assert(_connection.State == HubConnectionState.Connected);
                    return true;
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    {
                        _logger.LogExceptionWithNoLock($"Exception in connecting hub.", ex);
                        LogEvents($"Hub State :{_connection.State}");
                        // Failed to connect, trying again in 5000 ms.
                        Debug.Assert(_connection.State == HubConnectionState.Disconnected);
                        await Task.Delay(30000, cancellationToken);
                    }
                }
            }
        }

        private void LogEvents(string input)
        {
            _logger.LogWithNoLock($" {input}");
        }
    }

    public class TenSecondsRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(10);
        }
    }
}
