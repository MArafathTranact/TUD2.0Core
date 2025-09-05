using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.SignalR;
using TUDCoreService2._0.Utilities.Interface;
using TUDCoreService2._0.WebSocket;

namespace TUDCoreService2._0
{
    public class TudWorkerService : BackgroundService
    {
        private readonly INLogger _logger;
        private readonly IWebSocketListener _webSocketListener;
        private readonly ISignalRListener _signalRListener;
        private bool _runOnce = true;
        public TudWorkerService(INLogger logger, IWebSocketListener webSocketListener, ISignalRListener signalRListener)
        {
            _logger = logger;
            _webSocketListener = webSocketListener;
            _signalRListener = signalRListener;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            LogEvents($" Service Started ");
            LogEvents($" Version Number : 1.0.0 ");
            LogEvents($"-------- Maximum file size for the log is 100 MB --------");
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //LogEvents($" Service Started ");
            //LogEvents($" Version Number : 1.0.0 ");
            //LogEvents($"-------- Maximum file size for the log is 100 MB --------");
            try
            {
                //await _webSocketListener.ConnectWebSocket();
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_runOnce)
                    {
                        _runOnce = false;
                        //await _webSocketListener.ConnectWebSocket();
                        await _signalRListener.ConnectSignalR();
                    }
                }

                LogEvents($" Stoping Service..");
            }
            catch (Exception)
            {

            }
            finally
            {
                LogEvents($" Service Stopped ");
                NLog.LogManager.Shutdown();
            }

        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogEvents($" Stoping Service..");
                //await _webSocketListener.CloseWebSocket();
                await _signalRListener.CloseSignalR();
            }
            catch (Exception)
            {

            }
            finally
            {
                LogEvents($" Service Stopped ");
                NLog.LogManager.Shutdown();
            }

            //await base.StopAsync(cancellationToken);

        }


        //public async Task StartAsync(CancellationToken cancellationToken)
        //{
        //    LogEvents($" Service Started ");
        //    LogEvents($" Version Number : 1.0.0 ");
        //    LogEvents($"-------- Maximum file size for the log is 100 MB --------");
        //    try
        //    {
        //        await _webSocketListener.ConnectWebSocket();
        //        //await Task.Delay(1000, cancellationToken);
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //    await Task.Delay(1000, cancellationToken);
        //}

        //public async Task StopAsync(CancellationToken cancellationToken)
        //{
        //    LogEvents($" Stoping Service..");
        //    _webSocketListener.CloseWebSocket();
        //    LogEvents($" Service Stopped ");
        //    await Task.Delay(1000, cancellationToken);
        //    NLog.LogManager.Shutdown();
        //}

        private void LogEvents(string message)
        {
            _logger.LogWithNoLock(message);
        }
    }
}
