using Newtonsoft.Json;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using TUDCoreService2._0.Camera;
using TUDCoreService2._0.Models;
using TUDCoreService2._0.Scale_Reader;
using TUDCoreService2._0.Utilities;
using TUDCoreService2._0.Utilities.Interface;
using TUDCoreService2._0.WorkStation;
using static System.Collections.Specialized.BitVector32;
using static System.Formats.Asn1.AsnWriter;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TUDCoreService2._0.WebSocket
{
    public class WebSocketListener : IWebSocketListener
    {
        private readonly IAPI _aPI;
        private readonly IWorkStation _workStation;
        private IWorkStation workStation;
        private readonly ITUDSettings _tudSettings;
        private readonly IConfiguration _configuration;
        private IDictionary<string, IHandleScaleReader> _Scales;
        private readonly INLogger _logger;
        private string WorkStationIp;
        private string WorkStationPort;
        private string WorkStationName;
        private int WorkStationId = 0;
        private ClientWebSocket ws;
        private System.Timers.Timer refreshcameras = new System.Timers.Timer(1000 * 60 * 2);
        private System.Timers.Timer validateATMWebSocketPingPong = new System.Timers.Timer(1000 * 60 * 2);
        private DateTime PingPongTimeFromATMWebSocket = DateTime.Now;
        private bool ValidtWebSockettry = true;
        private readonly IHandleCamera _handleCamera;
        private readonly JpeggerCameraCaptureRequest _jpeggerCameraCaptureRequest;
        private readonly ICamera _camera;
        private readonly ICameraGroup _cameraGroup;
        private List<TudCommand> scaleSettingsCommand = [];
        #region Tcp Socket
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private ArrayList handlerList;
        private ArrayList socketList;
        private Socket listener;
        #endregion

        public WebSocketListener(
            IDictionary<string, IHandleScaleReader> scales,
            INLogger logger,
            ITUDSettings tudSettings,
            IConfiguration configuration,
            IAPI aPI,
            IWorkStation workStation,
            IHandleCamera handleCamera,
            ICamera camera,
            ICameraGroup cameraGroup)
        {
            _aPI = aPI;
            _logger = logger;
            _workStation = workStation;
            _tudSettings = tudSettings;
            _configuration = configuration;
            _Scales = scales;
            _handleCamera = handleCamera;
            _camera = camera;
            _cameraGroup = cameraGroup;
            _tudSettings = _configuration.GetSection("TUDSettings").Get<TUDSettings>();
            WorkStationIp = _tudSettings.WorkStationIp;
            WorkStationPort = _tudSettings.WorkStationPort;
            refreshcameras.Elapsed += new ElapsedEventHandler(RefreshCamerasEvent);
            refreshcameras.Start();
            validateATMWebSocketPingPong.Elapsed += new ElapsedEventHandler(ValidateWorkStationWebSocketPingPong);

        }


        #region Refresh and Websocket HeartBeat Check

        private async void RefreshCamerasEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                //LogEvents($"Refreshing Cameras/Groups");
                //await _camera.GetCameras();
                //await _cameraGroup.GetCameraGroups();
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.RefreshCamerasEvent.", ex);
            }
        }


        private async void ValidateWorkStationWebSocketPingPong(object source, ElapsedEventArgs e)
        {
            try
            {
                var difference = DateTime.Now.Subtract(PingPongTimeFromATMWebSocket).TotalMinutes;
                if (difference > 2)
                {
                    LogEvents($"No Ping message from Web Socket since {PingPongTimeFromATMWebSocket}");
                    ValidtWebSockettry = false;
                    validateATMWebSocketPingPong.Stop();
                    validateATMWebSocketPingPong.Enabled = false;
                    try
                    {
                        if (ws != null && ws.State == System.Net.WebSockets.WebSocketState.Open)
                        {
                            //await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                            ws.Abort();
                        }
                        if (ws != null)
                            ws.Dispose();
                        ws = null;

                    }
                    catch (Exception)
                    {
                    }

                    LogEvents($"Retrying to connect... {_tudSettings.WorkStationWebSocket}");
                    await Task.Delay(10000);
                    await ConnectWebSocket();
                }
                else
                {
                    ValidtWebSockettry = true;
                    LogEvents("Websocket heart beat in good condition");
                }

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.ValidateWorkStationWebSocketPingPong.", ex);
            }
        }

        #endregion

        #region WebSocket

        public async Task ConnectWebSocket()
        {
            try
            {
                await Task.Delay(3000);
                workStation = await GetWorkstations();
                if (workStation == null)
                {
                    _logger.LogWarningWithNoLock($" Work Station '{_tudSettings.WorkStationIp}' with Port {_tudSettings.WorkStationPort} is not found.");
                    return;
                }

                WorkStationName = workStation.name;
                WorkStationId = workStation.id;
                if (string.IsNullOrEmpty(_tudSettings.WorkStationWebSocket))
                {
                    _logger.LogWarningWithNoLock($" Work Station '{WorkStationName}' : Web Socket end point is not provided.");
                    return;
                }

                ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("Token", _tudSettings.JPEGgerToken);
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                await ws.ConnectAsync(new Uri(_tudSettings.WorkStationWebSocket), CancellationToken.None);
                LogEvents($"Web Socket connected... ");
                ValidtWebSockettry = true;
                var sending = Task.Run(async () =>
                {
                    try
                    {
                        var subscription = @"{""command"":""subscribe"", ""identifier"":""{\""channel\"":\""WorkstationChannel\"",\""ip\"":\""WorkStationIp\"",\""port\"":\""WorkStationPort\""}""}";
                        subscription = subscription.Replace("WorkStationIp", WorkStationIp);
                        subscription = subscription.Replace("WorkStationPort", WorkStationPort);


                        var bytes = Encoding.UTF8.GetBytes(subscription);
                        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
                        LogEvents($"Sending Web Socket subscription {subscription}");
                        if (ws != null && ws.State == WebSocketState.Open)
                        {
                            LogEvents($"Web Socket in Open state.Listening for Web Socket command... ");
                        }
                        else
                        {
                            LogEvents($"Web Socket Closed");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at Socket Subscription :", ex);
                    }

                });

                var receiving = Receiving(ws);
                var readConfig = ReadScaleSettingsFromLocalConfig();
                if (listener == null)
                {
                    var tcpListener = StartTcpListener();

                    await Task.WhenAll(sending, receiving, tcpListener, readConfig);
                }
                else if (listener != null && listener.IsConnected())
                {

                    await Task.WhenAll(sending, receiving, readConfig);
                }

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.ConnectWebSocket.", ex);
                try
                {
                    if (ws != null)
                    {
                        //await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        ws.Abort();
                        ws.Dispose();
                        ws = null;
                    }
                }
                catch (Exception)
                {
                }

                if (ValidtWebSockettry)
                {
                    LogEvents($"  Work Station '{WorkStationName}' : Retrying to connect... {_tudSettings.WorkStationWebSocket}");
                    await Task.Delay(15000);
                    await ConnectWebSocket();
                }
            }
        }

        private async Task Receiving(ClientWebSocket ws, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[2048];
            //var resu = "";

            validateATMWebSocketPingPong.Start();
            PingPongTimeFromATMWebSocket = DateTime.Now;

            try
            {
                while (ValidtWebSockettry)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var response = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        try
                        {
                            PingPongTimeFromATMWebSocket = DateTime.Now;

                            if (response.ToLowerInvariant().Contains("ping"))
                            {
                                try
                                {
                                    if (!response.ToLowerInvariant().Contains("confirm_subscription") && response.ToLowerInvariant().Contains("ping"))
                                    {
                                        //LogEvents($"Received Ping message from Web Socket ... {response} ");
                                        PingPongTimeFromATMWebSocket = DateTime.Now;
                                        var sending = Task.Run(async () =>
                                        {

                                            var pongMessage = @"{""command"":""message"", ""identifier"":""{\""channel\"":\""WorkstationChannel\"",\""id\"":\""workstationId\""}"",""data"":""{\""action\"":\""receive\"",\""type\"":\""pong\"",\""workstation_id\"":\""workstationId\""}""}";
                                            pongMessage = pongMessage.Replace("workstationId", WorkStationId.ToString());

                                            var bytes = Encoding.UTF8.GetBytes(pongMessage);
                                            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
                                            //LogEvents($"Sending Pong message to Web Socket ...{pongMessage} ");

                                        });

                                        await Task.WhenAll(sending);
                                    }
                                }
                                catch (Exception)
                                {


                                }
                            }
                            else
                            {
                                LogEvents($"Original Message : {response}");
                                var socket = JsonConvert.DeserializeObject<SocketMessage>(response);

                                if (socket == null) { }
                                else if (socket != null && socket.message == null) { }
                                else if (socket != null && socket.message != null && socket.message.command != null && socket.message.command.ToLower().Contains("error")) { }
                                else if (socket != null && socket.message != null && !string.IsNullOrEmpty(socket.message.command))
                                {
                                    var command = JsonConvert.DeserializeObject<TudCommand>(socket.message.command);
                                    if (command != null && !string.IsNullOrEmpty(command.scaleName))
                                    {
                                        LogEvents($"Received Command '{socket.message.command}'");
                                        LogEvents($"Command received to trigger Scale Reader '{command.scaleName}'");
                                        PingPongTimeFromATMWebSocket = DateTime.Now;
                                        if (!command.openClose)
                                        {
                                            var scaleName = command.id;
                                            if (!_Scales.ContainsKey(scaleName))
                                            {
                                                IHandleScaleReader scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);
                                                _Scales.Add(scaleName, scaleReaderHandler);
                                                scaleSettingsCommand.Add(command);
                                                await WriteScaleInformationToConfigFile();
                                                await scaleReaderHandler.ProcessCommandHandler(command, WorkStationName, WorkStationId, true, true);
                                            }
                                            else
                                            {
                                                if (_Scales.ContainsKey(scaleName))
                                                {
                                                    _Scales.TryGetValue(scaleName, out var scaleReaderHandler);
                                                    if (scaleReaderHandler != null)
                                                    {

                                                        await scaleReaderHandler.ProcessCommandHandler(command, WorkStationName, WorkStationId, true, false);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            IHandleScaleReader scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);
                                            await scaleReaderHandler.ProcessCommandHandler(command, WorkStationName, WorkStationId, true, false);
                                        }


                                    }
                                    else
                                    {

                                    }
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogExceptionWithNoLock($" Exception at Receiving", ex);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        ws.Abort();
                        break;
                    }

                    if (!ValidtWebSockettry)
                        break;
                }
            }
            catch (WebSocketException websocketException)
            {

                if (ws.State == WebSocketState.Closed)
                {


                }

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.Receiving.", ex);

                if (ws != null && ws.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    //await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    ws.Abort();
                }
                ws.Dispose();
                ws = null;
                if (ValidtWebSockettry)
                    await ConnectWebSocket();
            }
            //finally
            //{
            //    if (ws != null)
            //    {
            //        //await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            //        ws.Abort();
            //        ws.Dispose();
            //        ws = null;
            //        await Task.Delay(5000);
            //    }
            //}
        }

        public async Task CloseWebSocket()
        {
            try
            {
                try
                {
                    if (ws != null && ws.State == WebSocketState.Open)
                    {
                        ValidtWebSockettry = false;
                        //await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        ws.Abort();
                    }
                }
                catch (Exception)
                {

                }

                try
                {
                    if (listener != null)
                    {
                        //listener.Shutdown(SocketShutdown.Send);
                        //listener.Disconnect(true);
                        //listener.Dispose();
                    }
                }
                catch (Exception)
                {

                    throw;
                }

                foreach (var item in _Scales)
                {
                    try
                    {
                        var value = item.Value;
                        if (value != null)
                        {
                            value.CloseConnections();
                        }
                    }
                    catch (Exception)
                    {

                    }

                }
            }
            catch (Exception)
            {

            }

        }

        #endregion 

        #region Tcp Listener 
        public async Task StartTcpListener()
        {
            if (_tudSettings.EnableTcpPort == 1)
            {
                byte[] bytes = new Byte[1024];

                handlerList = new ArrayList();
                socketList = new ArrayList();

                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, _tudSettings.TcpPort);

                // Create a TCP/IP socket.
                listener = new Socket(AddressFamily.InterNetwork,
                     SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(100);
                    LogEvents($"Tcp Listener created at port {_tudSettings.TcpPort}");
                    LogEvents($"Waiting for a connection...");
                    while (true)
                    {
                        // Set the event to nonsignaled state.
                        allDone.Reset();

                        // Start an asynchronous socket to listen for connections.

                        listener.BeginAccept(
                            new AsyncCallback(AcceptCallback),
                            listener);

                        // Wait until a connection is made before continuing.
                        allDone.WaitOne();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.StartTcpListener ", ex);
                }
            }
            else
            {
                LogEvents($"Tcp Port not enabled.");
            }


        }

        public async void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Signal the main thread to continue.
                allDone.Set();

                // Get the socket that handles the client request.
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.
                StateObject state = new StateObject
                {
                    workSocket = handler
                };

                IPEndPoint remoteIpEndPoint = handler.RemoteEndPoint as IPEndPoint;
                LogEvents($"Connection in on {IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString())} , Port {((IPEndPoint)handler.RemoteEndPoint).Port}");
                LogEvents($"Waiting for a connection...");

                await ProcessTcpClientReadScale(handler);

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.StartListening ", ex);
            }
            //return Task.CompletedTask;

        }

        private async Task ProcessTcpClientReadScale(Socket handler)
        {
            try
            {
                var state = new StateObject { workSocket = handler };
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.ProcessTcpClientReadScale ", ex);
            }
        }

        private async void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            try
            {
                String request = String.Empty;
                String scaleWeight = String.Empty;



                // Read data from the client socket. 
                int bytesRead = handler.EndReceive(ar);
                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(
                      state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read 
                    // more data.
                    request = state.sb.ToString();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    };



                    var tcpMessage = System.Text.Json.JsonSerializer.Deserialize<Message>(request, options);//JsonConvert.DeserializeObject<Message>(request);
                    if (tcpMessage != null && !string.IsNullOrEmpty(tcpMessage.command))
                    {
                        scaleWeight = string.Empty;
                        var command = JsonConvert.DeserializeObject<TudCommand>(tcpMessage.command);
                        LogEvents($"Received Tcp Command '{JsonConvert.SerializeObject(tcpMessage)}'");


                        if (command != null && !command.openClose && command.isScaleSettingsUpdated)
                        {

                            LogEvents($"Tcp Command received to update scale settings..'{command.scaleName}'");
                            if (_Scales.ContainsKey(command.id))
                            {
                                try
                                {
                                    _Scales.TryGetValue(command.id, out var scaleReaderHandler);
                                    if (scaleReaderHandler != null)
                                    {
                                        _Scales.Remove(command.id);
                                        scaleSettingsCommand.RemoveAll(x => x.id == command.id);
                                        //scaleSettingsCommand.Add(command);
                                        //await WriteScaleInformationToConfigFile();
                                        scaleReaderHandler.CloseConnections();
                                        scaleReaderHandler = null;
                                    }

                                    IHandleScaleReader scaleReaderHandlerOnUpdate = new HandleScaleReader(_handleCamera, _logger, _aPI);

                                    _Scales.Add(command.id, scaleReaderHandlerOnUpdate);
                                    scaleSettingsCommand.Add(command);
                                    await WriteScaleInformationToConfigFile();
                                    scaleWeight = await scaleReaderHandlerOnUpdate.GetTcpScaleWeight(command, WorkStationName, WorkStationId, true);
                                }
                                catch (Exception)
                                {
                                }

                            }
                            else
                            {
                                IHandleScaleReader scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);

                                _Scales.Add(command.id, scaleReaderHandler);
                                scaleSettingsCommand.Add(command);
                                await WriteScaleInformationToConfigFile();
                                scaleWeight = await scaleReaderHandler.GetTcpScaleWeight(command, WorkStationName, WorkStationId, true);

                            }
                        }
                        else if (command != null && !command.openClose)
                        {
                            LogEvents($"Tcp Command received to trigger Scale Reader '{command.scaleName}'");
                            var scaleName = command.id;
                            if (!_Scales.ContainsKey(scaleName))
                            {
                                IHandleScaleReader scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);

                                _Scales.Add(scaleName, scaleReaderHandler);
                                scaleSettingsCommand.Add(command);
                                await WriteScaleInformationToConfigFile();
                                scaleWeight = await scaleReaderHandler.GetTcpScaleWeight(command, WorkStationName, WorkStationId, true);
                            }
                            else
                            {
                                if (_Scales.ContainsKey(scaleName))
                                {
                                    _Scales.TryGetValue(scaleName, out var scaleReaderHandler);
                                    if (scaleReaderHandler != null)
                                        scaleWeight = await scaleReaderHandler.GetTcpScaleWeight(command, WorkStationName, WorkStationId, false);
                                }
                            }
                        }
                        else if (command != null)
                        {
                            LogEvents($"Tcp Command received to trigger Scale Reader '{command.scaleName}'");
                            IHandleScaleReader scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);
                            scaleWeight = await scaleReaderHandler.GetTcpScaleWeight(command, WorkStationName, WorkStationId, false);
                        }

                        LogEvents($"Sending Tcp Response '{scaleWeight}'");
                        SendScaleResponse(handler, scaleWeight);
                    }
                }
                if (handler.Connected && !handler.IsConnected())
                {
                    LogEvents($"Client disconnected");
                    handler.Shutdown(SocketShutdown.Both);

                }
            }
            catch (SocketException sEx)
            {
                if (!handler.Connected)
                    LogEvents($"Client disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.ReadCallback ", ex);

                dynamic updateworkstationonScaleRead = new ExpandoObject();
                updateworkstationonScaleRead.Error = ex.Message;
                updateworkstationonScaleRead.Scale = 0;

                var response = new RemoteScaleResponse();
                //dynamic updateworkstationonScaleRead = new ExpandoObject();
                response.Error = ex.Message;


                var returnWeight = JsonConvert.SerializeObject(response);

                SendScaleResponse(handler, returnWeight);
            }
        }

        private void SendScaleResponse(Socket handler, String data)
        {
            try
            {
                //handler.Send(Encoding.UTF8.GetBytes(data));
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                       new AsyncCallback(SendCallback), handler);
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at WebSocketListener.SendScaleResponse ", ex);
            }


        }

        private void SendCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.
            Socket handler = (Socket)ar.AsyncState;
            try
            {


                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                if (bytesSent == 0)
                    LogEvents($"No result to send .");
                else
                    LogEvents($"Sent {bytesSent} bytes to client.");
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();

                StateObject state = new StateObject
                {
                    workSocket = handler
                };
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);


            }
            catch (SocketException sEX)
            {
                if (!handler.Connected)
                    LogEvents($"Client disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Work Station '{WorkStationName}' : Exception at SocketHandler.SendCallback.", ex);
            }
        }

        public async Task CloseTcpListener()
        {

        }


        #endregion 

        private async Task WriteScaleInformationToConfigFile()
        {
            try
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string pathname = Path.GetDirectoryName(path);

                string fileName = Path.Combine(pathname, "ScaleSettingsInfo.config");
                var scale = JsonConvert.SerializeObject(scaleSettingsCommand);
                if (!File.Exists(fileName))
                {
                    using FileStream fs = File.Create(fileName);

                    Byte[] info =
                        new UTF8Encoding(true).GetBytes(scale);

                    await fs.WriteAsync(info, 0, info.Length);
                }
                else
                {
                    using StreamWriter w = new StreamWriter(fileName, false);
                    await w.WriteLineAsync(scale);
                }
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at WriteScaleInformationToConfigFile().", ex);
            }
        }

        private async Task ReadScaleSettingsFromLocalConfig()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string pathname = Path.GetDirectoryName(path);

            string fileName = Path.Combine(pathname, "ScaleSettingsInfo.config");

            try
            {

                if (File.Exists(fileName))
                {
                    var lines = await File.ReadAllTextAsync(fileName);
                    if (!string.IsNullOrEmpty(lines))
                    {
                        var scalesSettings = JsonConvert.DeserializeObject<List<TudCommand>>(lines);

                        if (scalesSettings != null && scalesSettings.Any())
                        {
                            foreach (var setting in scalesSettings)
                            {
                                try
                                {
                                    scaleSettingsCommand.Add(setting);
                                    await ReadScaleOnServieStart(setting);

                                }
                                catch (Exception)
                                {
                                }
                            }

                        }
                    }
                }
                else
                {
                    LogEvents($"No config file found. Location ='{fileName}'");
                }

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at ReadScaleSettingsFromLocalConfig().", ex);
            }
        }

        private async Task ReadScaleOnServieStart(TudCommand command)
        {
            try
            {
                if (!command.openClose)
                {
                    LogEvents($"Triggerring Scale '{command.scaleName}' on service start");
                    var scaleName = command.id;
                    if (!_Scales.ContainsKey(scaleName))
                    {
                        IHandleScaleReader scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);
                        _Scales.Add(scaleName, scaleReaderHandler);
                        //scaleSettingsCommand.Add(command);
                        //await WriteScaleInformationToConfigFile();
                        await scaleReaderHandler.ProcessCommandHandler(command, WorkStationName, WorkStationId, false, true);
                    }
                    else
                    {
                        if (_Scales.ContainsKey(scaleName))
                        {
                            _Scales.TryGetValue(scaleName, out var scaleReaderHandler);
                            if (scaleReaderHandler != null)
                            {

                                await scaleReaderHandler.ProcessCommandHandler(command, WorkStationName, WorkStationId, false, false);
                            }
                        }
                    }
                }
                else
                {
                    IHandleScaleReader scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);
                    await scaleReaderHandler.ProcessCommandHandler(command, WorkStationName, WorkStationId, false, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at ReadScaleOnServieStart().", ex);
            }

        }
        private async Task<IWorkStation> GetWorkstations()
        {
            return _workStation.GetConfiguredWorkstation(_tudSettings.WorkStationIp, _tudSettings.WorkStationPort);
        }

        private void LogEvents(string input)
        {
            _logger.LogWithNoLock($" Work Station '{WorkStationName}' : {input}");
        }
    }
}
