using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using TUDCoreService2._0.Camera;
using TUDCoreService2._0.Models;
using TUDCoreService2._0.Utilities;
using System.Runtime.InteropServices.Marshalling;
using System.Text.RegularExpressions;
using TUDCoreService2._0.Utilities.Interface;
using ComObject = TUDCoreService2._0.Utilities.ComObject;
using System.Globalization;
using Newtonsoft.Json;
using System.Dynamic;

namespace TUDCoreService2._0.Scale_Reader
{
    public enum CurrentState
    {
        Tcp,
        Com,
        Udp
    }

    public class HandleScaleReader : IHandleScaleReader
    {

        string _workStationName = string.Empty;
        int _workStationId;
        private NetworkStream _networkStream;
        private TcpClient _tcpClient;
        private UdpClient _udpClient;
        private SerialPort _cPort;
        private bool _readingScale;
        private bool _scaleLocked;
        private bool? _isStopLightGreen;
        private string _errorMessage;
        private string _scaleOutput;
        public decimal ScaleWeight;
        private string CurrentWeight;
        private CurrentState CurrentState;
        private IHandleCamera _handleCamera;
        private INLogger _logger;
        private IAPI _aPI;
        private bool CloseConnection;
        public HandleScaleReader(IHandleCamera handleCamera, INLogger logger, IAPI aPI)
        {
            _handleCamera = handleCamera;
            _logger = logger;
            _aPI = aPI;
        }

        public async Task<string> GetTcpScaleWeight(TudCommand command, string workStationName, int workStationId)
        {
            var returnWeight = string.Empty;

            try
            {
                _workStationName = workStationName;
                CurrentWeight = null;
                _workStationId = workStationId;
                //_readingScale = true;
                var weightRead = await GetScaleWeight(command);

                if (string.IsNullOrEmpty(GetError()))
                {
                    if ((!decimal.TryParse(weightRead, out ScaleWeight) && weightRead != string.Empty) || ScaleWeight < 0)
                    {
                        LogEvents($"Error  Scale Value : {weightRead}");
                    }
                    else
                    {
                        //Can't guarantee the reading from the scale wasn't received with decimal so we will have to truncate it.
                        var formattedWeightRead = ScaleWeight.ToString("0.####", CultureInfo.InvariantCulture);
                        CurrentWeight = formattedWeightRead;
                        if (string.IsNullOrEmpty(CurrentWeight))
                            CurrentWeight = "0";
                    }

                    if (!string.IsNullOrEmpty(command.cameraName) && command.isFireCameraEnabled)
                    {
                        await TriggerCamera(command);
                    }
                }

                var response = new RemoteScaleResponse();
                dynamic updateworkstationonScaleRead = new ExpandoObject();
                response.Error = _errorMessage;
                response.Scale = CurrentWeight;
                returnWeight = JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleScaleReader.GetTcpScaleWeight", ex);
            }

            LogEvents($"Updating Scale Value : {returnWeight}");
            //_errorMessage = string.Empty;
            return returnWeight;
        }

        public async Task ProcessCommandHandler(TudCommand command, string workStationName, int workStationId, bool triggerUpdateCamera)
        {
            try
            {
                _workStationName = workStationName;
                _workStationId = workStationId;



                var weightRead = await GetScaleWeight(command);

                if (string.IsNullOrEmpty(GetError()))
                {
                    if ((!decimal.TryParse(weightRead, out ScaleWeight) && weightRead != string.Empty) || ScaleWeight < 0)
                    {
                        LogEvents($"Error  Scale Value : {weightRead}");
                    }
                    else
                    {
                        var formattedWeightRead = ScaleWeight.ToString("0.####", CultureInfo.InvariantCulture);
                        CurrentWeight = formattedWeightRead;
                    }

                    if (!string.IsNullOrEmpty(command.cameraName) && triggerUpdateCamera && command.isFireCameraEnabled)
                    {
                        await TriggerCamera(command);
                    }
                }

                await UpdateWorkStation(triggerUpdateCamera);
            }
            catch (Exception ex)
            {
                await UpdateWorkStation(triggerUpdateCamera);

                LogExceptionEvents("Exception at HandleScaleReader.ProcessCommandHandle", ex);
            }

            await Task.CompletedTask;
        }

        private async Task TriggerCamera(TudCommand command)
        {
            try
            {
                var request = new JpeggerCameraCaptureRequest
                {
                    CaptureDataApi = new JpeggerCameraCaptureDataModel
                    {
                        //   YardId = Guid.Parse(yardId), //Need to discuss on this
                        SpecifyJpeggerTable = "Images",
                        CommodityName = command.ticket != null ? command.commodity : "",
                        CameraName = command.cameraName,
                        TicketNumber = command.ticket != null ? command.ticket.ticket_nbr : "",
                        EventCode = command.ticket != null ? command.ticket.event_code : "",
                        Weight = CurrentWeight
                    },
                    // YardId = yardId, //Need to discuss on this
                    BranchCode = command.ticket != null ? command.branch_code : "",
                    CameraName = command.cameraName,
                    EventCode = command.ticket != null ? command.ticket.event_code : "",
                    TicketNumber = command.ticket != null ? command.ticket_nbr : "",
                    CommodityName = command.ticket != null ? command.commodity : "",
                    TransactionType = command.ticket != null ? command.transaction_type : "",
                    Weight = CurrentWeight
                };

                LogEvents($"Firing camera '{request.CameraName}'");
                await _handleCamera.TriggerCamera(request, _workStationName, _workStationId);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<string> GetScaleWeight(TudCommand command, bool nolock = false)
        {
            //try
            //{
            // nolock is only used at program startup
            if (!nolock)
            {
                if (await IsBeamBroken(command))
                {
                    return "Beam Broken";
                }
            }
            else
            {
                _readingScale = false;
                _scaleLocked = false;
            }

            if (_scaleLocked)
            {
                return "No Return To Zero";
            }
            if (!_readingScale)
            {
                if (!command.openClose)
                {
                    ReadScale(command);
                    //await Task.Delay(500);

                }
                else
                {
                    await ReadScale(command);
                }
            }
            if (!nolock && (decimal.TryParse(_scaleOutput, out _)))
            {
                //intentionally not awaited
                ReturnToWeightCheck(command);
            }
            //}
            //catch (Exception ex)
            //{
            //    LogExceptionEvents("Exception at HandleScaleReader.GetScaleWeight", ex);
            //}

            return _scaleOutput;
        }

        private async Task<bool> IsBeamBroken(TudCommand command)
        {
            if (!command.isBeamMonitorEnabled
                || command.beamMonitorEndpoint?.Length == 0
                || (command.disabledUntilAfterDate != null && command.disabledUntilAfterDate > DateTime.Now))
            {
                return false;
            }

            XmlReaderSettings settings = new XmlReaderSettings { Async = true };

            string inputUri = UrlPathHelper.Combine(command.beamMonitorEndpoint, "state.xml");

            //using var xmlRader = XmlReader.Create(inputUri, settings);
            using (var xmlRader = XmlReader.Create(inputUri, settings))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(WebRelay));
                    var response = (WebRelay)serializer.Deserialize(xmlRader);
                    return response.input2state == 1;
                }
                catch (Exception ex)
                {
                    LogExceptionEvents("Exception at HandleScaleReader.IsBeamBroken", ex);
                    return false;
                }
            }

        }

        private async Task ReadScale(TudCommand command)
        {
            //try
            //{
            if (!command.openClose && !_scaleLocked && !_readingScale)
            {
                _readingScale = true;

                if (command.useIpAddress && !string.IsNullOrEmpty(command.ipAddress) && command.ipPort.HasValue && command.ipPort != 0)
                {
                    LogEvents($"Using TCP Ip to Read scale value using '{command.ipAddress}' at port '{command.ipPort}'");
                    CurrentState = CurrentState.Tcp;
                }
                else
                {
                    LogEvents($"Using Com Port to Read scale value using 'COM{command.comPort}'");
                    CurrentState = CurrentState.Com;
                }

                while ((_readingScale || _scaleLocked))
                {
                    if (CloseConnection)
                        break;
                    await ReadScaleWeight(command);
                    if (int.TryParse(_scaleOutput, out var scaleWeight))
                    {
                        await ControlStopLight(scaleWeight < command.returnWeight, command);
                    }
                }
            }
            else if (!_readingScale)
            {
                if (command.useIpAddress && !string.IsNullOrEmpty(command.ipAddress) && command.ipPort.HasValue && command.ipPort != 0)
                {
                    LogEvents($"Using TCP Ip to Read scale value using '{command.ipAddress}' at port '{command.ipPort}'");
                    CurrentState = CurrentState.Tcp;
                }
                else
                {
                    LogEvents($"Using Com Port to Read scale value using 'COM{command.comPort}'");
                    CurrentState = CurrentState.Com;
                }

                await ReadScaleWeight(command);
                _readingScale = false;
            }
            //}
            //catch (Exception ex)
            //{
            //    LogExceptionEvents("Exception at HandleScaleReader.ReadScale", ex);
            //}

        }

        private async Task ControlStopLight(bool isStopLightGreen, TudCommand command)
        {
            try
            {
                if (isStopLightGreen == _isStopLightGreen
      || !command.isStopLightEnabled
      || string.IsNullOrEmpty(command.stopLightEndpoint))
                {
                    return;
                }

                _isStopLightGreen = isStopLightGreen;
                string inputUri = UrlPathHelper.Combine(command.stopLightEndpoint, "state.xml");
                var state = isStopLightGreen ? "1" : "0";
                string myParameters = $"?relayState={state}&noReply=0";
                try
                {
                    using (var _httpClient = new HttpClient())
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, $"{inputUri}{myParameters}");
                        var reply = await _httpClient.SendAsync(request);
                    }
                }
                catch (Exception ex)
                {
                    LogExceptionEvents("Exception at HandleScaleReader.ControlStopLight.InnerException", ex);
                }
            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleScaleReader.ControlStopLight.OutterException", ex);
            }

        }

        private async Task ReadScaleWeight(TudCommand command)
        {
            //try
            //{
            await Task.Run(
                         async () =>
                         {
                             try
                             {
                                 if (command.useIpAddress && !string.IsNullOrEmpty(command.ipAddress) && command.ipPort.HasValue && command.ipPort != 0)
                                     ReadTcp(command);
                                 else
                                     ReadCom(command);
                             }
                             catch (Exception ex)
                             {
                                 //LogExceptionEvents(" Exception at HandleScaleReader.ReadScaleWeight ", ex);
                                 _errorMessage = ex.Message.ToString();

                                 await Task.Delay(TimeSpan.FromSeconds(2));
                             }
                         });
            //}
            //catch (Exception ex)
            //{
            //    LogExceptionEvents("Exception at HandleScaleReader.ReadScaleWeight", ex);
            //    _errorMessage = ex.Message.ToString();
            //}

        }

        private async Task ReturnToWeightCheck(TudCommand command)
        {
            try
            {
                await Task.Run(
                      async () =>
                      {
                          if (_scaleLocked)
                          {
                              return;
                          }

                          if (!command.isReturnWeightEnabled)
                          {
                              _scaleLocked = false;
                              return;
                          }

                          _scaleLocked = true;
                          var weigthRead = 0m;
                          while (_scaleLocked)
                          {
                              if (CloseConnection)
                                  break;

                              if (decimal.TryParse(_scaleOutput, out weigthRead))
                              {
                                  if (command.returnWeight >= weigthRead)
                                  {
                                      _scaleLocked = false;
                                      if (command.useIpAddress)
                                      {
                                          CloseNetworkStream();
                                      }

                                      break;
                                  }

                                  if (!_readingScale)
                                  {
                                      await ReadScaleWeight(command);
                                  }
                              }
                              else if (!_readingScale)
                              {
                                  await ReadScaleWeight(command);
                              }
                          }


                      });

            }
            catch (Exception ex)
            {
                //LogExceptionEvents("Exception at HandleScaleReader.ReturnToWeightCheck", ex);
            }

        }

        private void ReadTcp(TudCommand command)
        {
            //try
            //{
            var charlist = new List<int>();
            var i = 0;
            var matchReads = 0;
            var lastRead = string.Empty;

            if (_networkStream == null
                || _tcpClient == null
                || !_tcpClient.Connected)
            {
                OpenNetworkStream(command);
            }

            if (_tcpClient == null || !_tcpClient.Connected)
            {
                return;
            }

            _networkStream.Flush();
            while (i < command.maxCharToRead)
            {
                var sb = new StringBuilder();

                var currentCharacter = Convert.ToChar(_networkStream.ReadByte());

                //if (DataLogging)
                //{
                //    _scaleDebugData.Add(
                //        new ScaleDebugDataModel
                //        {
                //            Character = currentCharacter,
                //            Position = 0,
                //            AscChar = Convert.ToChar(currentCharacter).ToString(),
                //            WeightString = string.Empty
                //        });
                //}

                if (currentCharacter == command.startOfText)
                {
                    while (i < command.maxCharToRead)
                    {
                        charlist.Add(currentCharacter);
                        //if (DataLogging)
                        //{
                        //    _scaleDebugData.Add(
                        //        new ScaleDebugDataModel
                        //        {
                        //            Character = currentCharacter,
                        //            Position = charlist.Count, // - 1,
                        //            AscChar = Convert.ToChar(currentCharacter).ToString(),
                        //            WeightString = sb.Append(Convert.ToChar(currentCharacter)).ToString()
                        //        });
                        //}

                        currentCharacter = Convert.ToChar(_networkStream.ReadByte());

                        if (currentCharacter == command.startOfText)
                        {
                            var nextRead = FormatWeight(charlist, command);
                            if (nextRead == lastRead)
                            {
                                matchReads++;
                            }
                            else
                            {
                                matchReads = 0;
                            }

                            if (matchReads >= command.numberOfMatchingRead)
                            {
                                i = command.maxCharToRead ?? 150;
                            }

                            lastRead = nextRead;
                        }
                        else
                            i++;
                    }
                }
                else
                    i++;
            }

            _scaleOutput = lastRead;

            if (command.openClose
              && !_readingScale
              && !_scaleLocked)
            {
                CloseNetworkStream();
            }

            //}
            //catch (Exception ex)
            //{
            //    _errorMessage = ex.Message.ToString();
            //    //LogExceptionEvents("Exception at HandleScaleReader.ReadTcp", ex);
            //}


        }

        private void CloseNetworkStream()
        {
            try
            {
                LogEvents($"Closing Tcp Network stream ");
                _readingScale = false;
                if (_networkStream != null)
                {
                    _networkStream.Close(0);
                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleScaleReader.CloseNetworkStream", ex);
            }

        }

        private void OpenNetworkStream(TudCommand command)
        {
            //try
            //{
            IPAddress ipAddress = IPAddress.Parse(command.ipAddress);
            IPEndPoint endpoint = new IPEndPoint(ipAddress, command.ipPort.Value);
            //LogEvents($"Opening stream for Ip '{command.ipAddress}' at port '{command.ipPort}'");
            if (_tcpClient == null
                || !_tcpClient.Connected)
            {
                _tcpClient = new TcpClient();
                // _tcpClient.Connect(endpoint);

                if (!_tcpClient.ConnectAsync(command.ipAddress, command.ipPort.Value).Wait(4000))
                {
                    _errorMessage = $"No connection could be made because the target machine actively refused it {command.ipAddress}:{command.ipPort}";
                    return;
                }


            }

            _networkStream = _tcpClient.GetStream();
            //}
            //catch (Exception ex)
            //{
            //    _errorMessage = ex.Message.ToString();
            //    //LogExceptionEvents("Exception at HandleScaleReader.OpenNetworkStream", ex);
            //}

        }

        private void ReadCom(TudCommand camera)
        {
            try
            {
                //LogEvents($"Reading scale weight..");
                var charlist = new List<int>();
                var i = 0;
                var matchReads = 0;
                var lastRead = string.Empty;

                InitializeComPort(camera);

                if (_cPort == null)
                {
                    _errorMessage = $"No com port initialized for 'Com{camera.comPort}'.";
                    //LogEvents($"No com port initialized for 'Com{camera.comPort}'.");
                    return;
                }
                if (!_cPort.IsOpen)
                {
                    _cPort.Open();
                }

                _cPort.DiscardInBuffer();
                _cPort.DiscardOutBuffer();


                while (i < camera.maxCharToRead)
                {
                    var sb = new StringBuilder();
                    var currentCharacter = _cPort.ReadChar();

                    if (currentCharacter == camera.startOfText)
                    {
                        while (i < camera.maxCharToRead)
                        {
                            charlist.Add(currentCharacter);
                            {

                            }

                            currentCharacter = _cPort.ReadChar();
                            if (currentCharacter == camera.startOfText)
                            {
                                var nextRead = FormatWeight(charlist, camera);
                                if (nextRead == lastRead)
                                {
                                    matchReads++;
                                }
                                else
                                {
                                    matchReads = 0;
                                }

                                if (matchReads >= camera.numberOfMatchingRead)
                                {
                                    i = camera.maxCharToRead ?? 150;
                                }

                                lastRead = nextRead;
                            }
                            else
                                i++;
                        }
                    }
                    else
                        i++;
                }

                _scaleOutput = lastRead;
                if (camera.openClose
               && !_readingScale
               && !_scaleLocked)
                {
                    CloseComPort();
                }

            }
            catch (System.IO.IOException ex)
            {
                LogEvents($"Com port '{camera.comPort}' is being closed.");
            }
            catch (OperationCanceledException ex)
            {
                _errorMessage = ex.Message.ToString();
                //LogExceptionEvents("Exception at HandleScaleReader.ReadCom", ex);
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message.ToString();
                if (_errorMessage.Contains("The operation has timed out"))
                {
                    CloseComPort();
                }
                //LogExceptionEvents("Exception at HandleScaleReader.ReadCom", ex);
            }

        }

        private async Task ReadUdp(TudCommand command)
        {
            try
            {
                var charlist = new List<int>();
                var matchReads = 0;
                var lastRead = string.Empty;

                if (_udpClient == null
                    || _udpClient.Client == null
                    || !_udpClient.Client.IsBound)
                {
                    OpenUdp(command);
                }

                var udpOutput = await ReadUdpString(command);
                var startCharFound = false;
                var sb = new StringBuilder();

                foreach (var currentCharacter in udpOutput.ToCharArray())
                {
                    //if (DataLogging && !startCharFound)
                    //{
                    //    _scaleDebugData.Add(
                    //        new ScaleDebugDataModel
                    //        {
                    //            Character = currentCharacter,
                    //            Position = 0,
                    //            AscChar = Convert.ToChar(currentCharacter).ToString(),
                    //            WeightString = string.Empty
                    //        });
                    //}

                    if (currentCharacter == command.startOfText || startCharFound)
                    {
                        charlist.Add(currentCharacter);
                        //if (DataLogging)
                        //{
                        //    _scaleDebugData.Add(
                        //        new ScaleDebugDataModel
                        //        {
                        //            Character = currentCharacter,
                        //            Position = charlist.Count, // - 1,
                        //            AscChar = Convert.ToChar(currentCharacter).ToString(),
                        //            WeightString = sb.Append(Convert.ToChar(currentCharacter)).ToString()
                        //        });
                        //}

                        if (currentCharacter == command.startOfText && startCharFound)
                        {
                            var nextRead = FormatWeight(charlist, command);
                            if (nextRead == lastRead)
                            {
                                matchReads++;
                            }
                            else
                            {
                                matchReads = 0;
                            }

                            if (matchReads >= command.numberOfMatchingRead)
                            {
                                lastRead = nextRead;
                                break;
                            }

                            lastRead = nextRead;
                        }

                        startCharFound = true;
                    }
                }
                _scaleOutput = lastRead;
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message.ToString();
                LogExceptionEvents("Exception at HandleScaleReader.ReadUdp", ex);
            }

        }

        private void OpenUdp(TudCommand command)
        {
            if (_udpClient == null
                || _udpClient.Client == null
                || !_udpClient.Client.IsBound)
            {
                _udpClient = new UdpClient();
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, command.ipPort.Value));
            }
        }

        private async Task<string> ReadUdpString(TudCommand command)
        {
            var sb = new StringBuilder();

            while (sb.Length < command.maxCharToRead)
            {
                var result = await _udpClient.ReceiveAsync();
                sb.Append(Encoding.UTF8.GetString(result.Buffer));
            }

            return sb.ToString();
        }

        private string FormatWeight(List<int> charlist, TudCommand camera)
        {
            var unitsPos = (camera.unitsPosition ?? 1) - 1;
            var modePos = (camera.modePosition ?? 1) - 1;
            var motionPos = (camera.motionPosition ?? 1) - 1; //Subtract 1 since charlist index starts at 0
            var sb = new StringBuilder();
            try
            {
                if (charlist.Count <= unitsPos)
                {
                    _errorMessage = "Input not long enough to check Unit of Measure character";
                    return "Invalid";
                }
                else if (charlist[unitsPos] != (camera.lbUnitsChar ?? 0))
                {
                    _errorMessage = "Incorrect Unit of Measure character.";
                    return "Invalid";
                }

                if (charlist.Count <= modePos)
                {
                    _errorMessage = "Input not long enough to check Gross Mode character";
                    return "Invalid";
                }
                else if (charlist[modePos] != (camera.grossModeChar ?? 0))
                {
                    _errorMessage = "Incorrect Gross Mode character";
                    return "Invalid";
                }

                if (charlist.Count <= motionPos)
                {
                    _errorMessage = "Input not long enough to check Motion character";
                    return "Invalid";
                }
                else if (charlist[motionPos] != (camera.noMotionChar ?? 0))
                {
                    _errorMessage = "Incorrect Motion character";
                    return "Invalid";
                }

                var parts = charlist.GetRange(((camera.weightBeginPosition ?? 1) - 1), ((camera.weightEndPosition ?? 1) + 1 - (camera.weightBeginPosition ?? 1)));

                foreach (var item in parts)
                {
                    sb.Append(Convert.ToChar(item));
                }

            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleScaleReader.FormatWeight", ex);
            }

            // Clear Previous Errors
            _errorMessage = string.Empty;

            return sb.ToString().Trim();
        }

        private void CloseComPort()
        {
            try
            {
                if (_cPort != null
                               && _cPort.IsOpen)
                {
                    _readingScale = false;
                    LogEvents($"Closing Com Port...");
                    _cPort.Close();
                }
            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleScaleReader.CloseComPort", ex);
            }
        }

        private void InitializeComPort(TudCommand camera)
        {
            //try
            //{
            //LogEvents($"Initializing Com Port at {camera.comPort}...");
            _cPort = ComObject.InitializeCom(
                new ComDefinitionModel
                {
                    BaudRate = camera.baudRate,
                    DataBits = Convert.ToInt32(Regex.Split(camera.dataStop, "/")[0]),
                    StopBits = (StopBits)Convert.ToInt32(Regex.Split(camera.dataStop, "/")[1]),
                    Parity = (Parity)camera.scaleParity,
                    PortName = "COM" + camera.comPort,
                    ReadTimeout = 3000
                });
            //}
            //catch (System.FormatException ex)
            //{
            //    _errorMessage = ex.Message.ToString();
            //    //LogExceptionEvents("Exception at HandleScaleReader.InitializeComPort", ex);
            //}
        }

        public string GetError()
        {
            return _errorMessage;
        }
        public void CloseConnections()
        {
            try
            {
                //_readingScale = false;
                //_scaleLocked = false;
                CloseConnection = true;
                if (CurrentState == CurrentState.Tcp)
                    CloseNetworkStream();
                else if (CurrentState == CurrentState.Com)
                    CloseComPort();
            }
            catch (Exception)
            {

            }
        }
        private void LogExceptionEvents(string input, Exception exception)
        {
            _logger.LogExceptionWithNoLock($" Work Station '{_workStationName}' : {input} :", exception);
        }

        private async Task UpdateWorkStation(bool updateAPI)
        {
            if (string.IsNullOrEmpty(CurrentWeight))
                CurrentWeight = "0";

            dynamic updateworkstationonScaleRead = new ExpandoObject();
            updateworkstationonScaleRead.Error = _errorMessage;
            updateworkstationonScaleRead.Scale = CurrentWeight;
            var json = JsonConvert.SerializeObject(updateworkstationonScaleRead);
            var updateWorkStation = new UpdateWorkStation() { command = json };

            LogEvents($"Updating Scale Value : {JsonConvert.SerializeObject(updateWorkStation)}");
            LogEvents($"Update status {updateAPI}");
            if (updateAPI)
            {
                await _aPI.PutRequest<UpdateWorkStation>(updateWorkStation, $"workstations/{_workStationId}");
            }
            //_errorMessage = string.Empty;

        }
        private void LogEvents(string input)
        {
            _logger.LogWithNoLock($" Work Station '{_workStationName}' : {input}");
        }


    }
}
