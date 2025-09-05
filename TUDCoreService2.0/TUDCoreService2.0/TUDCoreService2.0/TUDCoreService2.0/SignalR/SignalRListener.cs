using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Serialization;
using TUDCoreService2._0.Camera;
using TUDCoreService2._0.Models;
using TUDCoreService2._0.Scale_Reader;
using TUDCoreService2._0.Utilities;
using TUDCoreService2._0.Utilities.Interface;
using TUDCoreService2._0.WorkStation;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace TUDCoreService2._0.SignalR
{
    public class SignalRListener : ISignalRListener
    {
        #region Properties




        private GraphQLHttpClient _client;
        private CancellationTokenSource _cts;
        private readonly IAPI _aPI;
        private readonly IWorkStation _workStation;


        private IDictionary<string, IHandleScaleReader> _Scales;
        private readonly INLogger _logger;
        private readonly IMapper _mapper;
        private readonly IAPIConnection _aPIConnection;
        private ClientWebSocket ws;

        private readonly IHandleCamera _handleCamera;
        private readonly JpeggerCameraCaptureRequest _jpeggerCameraCaptureRequest;
        private List<TudCommand> scaleSettingsCommand = [];
        #region Tcp Socket
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private ArrayList handlerList;
        private ArrayList socketList;
        private Socket listener;
        private ConcurrentQueue<OnPortalScaleReadRequestReceivedPayload> messageQueue = new();
        private readonly HubConnection _connection;
        private readonly ISubscriptionHubClient _subscriptionHubClient;
        #endregion
        #endregion Properties


        public SignalRListener(
            IDictionary<string, IHandleScaleReader> scales,
            INLogger logger,
            IAPIConnection aPIConnection,
            IAPI aPI,
            IHandleCamera handleCamera,
            ICameraGroup cameraGroup,
            IMapper mapper,
            ISubscriptionHubClient subscriptionHubClient)
        {
            _aPIConnection = aPIConnection;
            _aPI = aPI;
            _logger = logger;

            _Scales = scales;
            _handleCamera = handleCamera;
            _mapper = mapper;
            _subscriptionHubClient = subscriptionHubClient;

            _cts = new CancellationTokenSource();
        }

        public async Task ConnectSignalR()
        {
            await Task.Delay(3000);
            try
            {
                SubscribeToSignalRPortalScaleReadRequest();

                await Task.Run(async () => await GetSignalRSubscription());
                await Task.Run(async () => await DequeueSignalRMessage(_cts.Token));

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at GetSignalRSubscription().", ex);
            }

        }

        private async Task GetSignalRSubscription(CancellationToken cancellationToken = default)
        {
            try
            {
                await _subscriptionHubClient.StartAsync(cancellationToken);
                ReadScaleSettingsFromConfig();

                if (_subscriptionHubClient != null && _subscriptionHubClient._connection != null && _subscriptionHubClient._connection.State == HubConnectionState.Connected)
                    LogEvents($"Connected to hub.");
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at GetSignalRSubscription().", ex);
            }

        }

        private void SubscribeToSignalRPortalScaleReadRequest()
        {
            try
            {

                if (_subscriptionHubClient?._connection is HubConnection connection)
                {
                    connection.On<OnPortalScaleReadRequestReceivedPayload>(
                        "OnPortalScaleReadRequestReceived",
                        scalePayload =>
                        {
                            LogEvents($"Received payload {System.Text.Json.JsonSerializer.Serialize(scalePayload)}");

                            messageQueue.Enqueue(scalePayload);
                            LogEvents($"Enqueued Scale : {scalePayload.ScaleId}");
                        });
                }


                //if (_subscriptionHubClient != null && _subscriptionHubClient._connection != null)
                //{
                //    _subscriptionHubClient._connection.On<OnPortalScaleReadRequestReceivedPayload>("OnPortalScaleReadRequestReceived", async (scalePayload) =>
                //    {
                //        LogEvents($"Received payload {System.Text.Json.JsonSerializer.Serialize(scalePayload)}");

                //        messageQueue.Enqueue(scalePayload);
                //        LogEvents($"Enqueuing Scale : {scalePayload.ScaleId}");
                //        Observable
                //        .FromAsync(() => DequeueSignalRMessage())
                //        .Subscribe(async response =>
                //        {

                //            LogEvents($"Waiting for scale to read value.");

                //        });

                //    });

                //}
            }
            catch (Exception ex)
            {

                _logger.LogExceptionWithNoLock("Exception at SendScaleValueToPortal().", ex);
            }
        }


        private async Task DequeueSignalRMessage(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (messageQueue.TryDequeue(out var payload))
                {
                    try
                    {
                        LogEvents($"Processing scale {payload.ScaleId}");
                        LogEvents($"Dequeuing Scale : {payload.ScaleId}");
                        await GetScaleValueFromHandler(payload);
                        //await DequeueSignalRMessage(); // <- put payload handling inside
                    }
                    catch (Exception ex)
                    {
                        _logger.LogExceptionWithNoLock("Failed to process scale payload.", ex);
                    }
                }
                else
                {
                    await Task.Delay(100, token); // avoid busy loop
                }
            }
        }


        //private async Task<bool> DequeueSignalRMessage()
        //{
        //    try
        //    {
        //        if (messageQueue.TryDequeue(out OnPortalScaleReadRequestReceivedPayload payload))
        //        {
        //            LogEvents($"Dequeuing Scale : {payload.ScaleId}");
        //            await GetScaleValueFromHandler(payload);
        //        }
        //        return true;

        //    }
        //    catch (Exception ex)
        //    {
        //        return true;
        //    }
        //}


        private async Task GetScaleValueFromHandler(OnPortalScaleReadRequestReceivedPayload scalePayload)
        {
            Observable
                   .FromAsync(async () =>
                   {
                       var result = new RemoteScaleResponse() { ScaleId = scalePayload.ScaleId };
                       try
                       {
                           var scaleIdKey = scalePayload.ScaleId.ToString();

                           var scaleCommand = scaleSettingsCommand
                               .FirstOrDefault(x => x.id == scaleIdKey);

                           if (scaleCommand is null)
                           {
                               result.Error = "Scale info not found";
                               return result;
                           }

                           LogEvents($"Scale Info for '{scaleCommand.id}' : {System.Text.Json.JsonSerializer.Serialize(scaleCommand)}");

                           result.ScaleName = scaleCommand.scaleName;
                           scaleCommand.ticket_nbr = scalePayload.TicketNumber;
                           scaleCommand.yardId = scalePayload.YardId.ToString();
                           scaleCommand.event_code = scalePayload.EventCode;
                           scaleCommand.commodity = scalePayload.Commodity;
                           scaleCommand.tare_seq_nbr = scalePayload.TareSeqNumber;
                           scaleCommand.amount = scalePayload.Amount;

                           IHandleScaleReader scaleReaderHandler;

                           if (!scaleCommand.openClose && scaleCommand.isScaleSettingsUpdated)
                           {
                               // Either reuse or create new
                               if (!_Scales.TryGetValue(scaleIdKey, out scaleReaderHandler))
                               {
                                   scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);
                                   _Scales[scaleIdKey] = scaleReaderHandler;

                                   result = await scaleReaderHandler.ProcessSignalRCommandHandler(scaleCommand, true, true);
                               }
                               else
                               {
                                   result = await scaleReaderHandler.ProcessSignalRCommandHandler(scaleCommand, true, false);
                               }
                           }
                           else
                           {
                               // Always create new handler in this case
                               scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);
                               result = await scaleReaderHandler.ProcessSignalRCommandHandler(scaleCommand, true, false);
                           }



                           return result;
                       }
                       catch (Exception ex)
                       {
                           result.Error = ex.Message;
                           _logger.LogExceptionWithNoLock("Exception at GetScaleValueFromHandler().", ex);
                           return result;
                       }


                   })
                   .Subscribe(async response =>
                   {
                       try
                       {
                           LogEvents($"Scale='{scalePayload.ScaleId}', YardId='{scalePayload.YardId}', Error={response.Error}, Scale Value={response.Scale}");


                           await SendScaleValue(response);// _subscriptionHubClient._connection.InvokeAsync("ReadScaleValueFromService", response.Error);
                       }
                       catch (Exception ex)
                       {
                           _logger.LogExceptionWithNoLock($" Scale = ({scalePayload.ScaleId}) : Exception occured in sending response to Hub caller.", ex);
                       }

                   });

        }


        private async Task ReadScaleSettingsFromConfig()
        {
            try
            {

                var endpoint = _aPIConnection.GetEndPoint();
                var token = _aPIConnection.GetToken();
                var settingsId = _aPIConnection.GetSettingsId();

                await ReadGraphqlScaleInformation(endpoint, token, settingsId, _cts.Token);


            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at ReadScaleSettingsFromLocalConfig().", ex);
            }
        }

        private async Task ReadGraphqlScaleInformation(string endPoint, string token, List<string> settingsId, CancellationToken cancellationToken)
        {
            try
            {

                if (!string.IsNullOrEmpty(endPoint) && !string.IsNullOrEmpty(token) && settingsId != null && settingsId.Any())
                {
                    var orConditions = settingsId
                        .Select(id => new
                        {
                            settingsId = new
                            {
                                eq = id
                            }
                        })
                        .ToArray();


                    var decryptedToken = TokenEncryptDecrypt.Decrypt(token);

                    _client = new GraphQLHttpClient(endPoint + "/sdinternal/graphql/", new NewtonsoftJsonSerializer());
                    _client.HttpClient.DefaultRequestHeaders.Add("X-api-key", decryptedToken);
                    _client.HttpClient.DefaultRequestHeaders.Add("X-sdinternal-bypass", "SDInternalTesting");

                    var request = new GraphQLRequest
                    {
                        Query = @"
                        query searchLocalComputerScales($filter: LocalComputerScaleFilterInput!)
                                {
                                localComputerScales(where: $filter)
                                    {
                                        nodes {
                                            ...LocalComputerScaleModel
                                              }
                                    }
                                }   

                        fragment LocalComputerScaleModel on LocalComputerScale {
                            id
                            ipAddress
                            ipPort
                            settingsId
                            scaleName
                            cameraName
                            comPort
                            baudRate
                            dataStop
                            scaleParity
                            bufferSize
                            weightBeginPosition
                            weightEndPosition
                            motionPosition
                            unitsPosition
                            modePosition
                            startOfText
                            noMotionChar
                            lbUnitsChar
                            grossModeChar
                            maxCharToRead
                            numberOfMatchingRead
                            useIpAddress
                            ipAddress
                            ipPort
                            isBeamMonitorEnabled
                            beamMonitorEndpoint
                            disabledUntilAfterDate
                            isReturnWeightEnabled
                            returnWeight
                            isStopLightEnabled
                            openClose
                            stopLightEndpoint           
                        }
                        ",
                        Variables = new
                        {
                            filter = new
                            {
                                and = new object[]
                                 {
                                    new
                                    {
                                        isDisabled = new
                                        {
                                            eq = false
                                        }
                                    },
                                    new
                                    {
                                        or = orConditions
                                    }
                                }
                            }
                        }
                    };

                    var response = await _client.SendQueryAsync<dynamic>(request, cancellationToken);

                    if (response.Errors != null && response.Errors.Any())
                    {
                        LogEvents($"Error received for Graphql call : {response.Errors}");
                        return;
                    }
                    string json = JsonConvert.SerializeObject(response.Data);


                    var data = JsonConvert.DeserializeObject<GraphQLResponseData>(json);

                    if (data != null && data.localComputerScales != null && data.localComputerScales.nodes != null)
                    {
                        var tudCommand = _mapper.Map<List<TudCommand>>(data.localComputerScales.nodes);

                        LogEvents($"Total scale reeived : {tudCommand.Count}");
                        foreach (var setting in tudCommand)
                        {
                            try
                            {
                                scaleSettingsCommand.Add(setting);
                                ReadScaleOnServieStart(setting);

                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                else
                    LogEvents($"No valid configuration information found.");

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at ReadGraphqlScaleInformation().", ex);
            }
        }

        private async Task SendScaleValue(RemoteScaleResponse scaleResponse)
        {
            try
            {
                if (_client == null)
                {
                    var endpoint = _aPIConnection.GetEndPoint();
                    var token = _aPIConnection.GetToken();
                    var settingsId = _aPIConnection.GetSettingsId();

                    var decryptedToken = TokenEncryptDecrypt.Decrypt(token);

                    _client = new GraphQLHttpClient(endpoint + "/sdinternal/graphql/", new NewtonsoftJsonSerializer());
                    _client.HttpClient.DefaultRequestHeaders.Add("X-api-key", decryptedToken);
                    _client.HttpClient.DefaultRequestHeaders.Add("X-sdinternal-bypass", "SDInternalTesting");
                }


                var mutation = new GraphQLRequest
                {
                    Query = @"
                      mutation ($input: OnPortalScaleReadResponseReceivedPayloadInput!) {
                             readTicketItemScaleValueResponse(input: $input) {
                                 status
                                }
                             }",
                    Variables = new
                    {
                        input = new
                        {
                            scaleId = scaleResponse.ScaleId,
                            error = "",
                            status = true,
                            scale = "125.00"
                        }
                    }
                };

                var response = await _client.SendMutationAsync<dynamic>(mutation);

                if (response != null && response.Errors != null && response.Errors.Any())
                {
                    LogEvents($"Scale '{scaleResponse.ScaleId}' : Error received for Graphql call posting scale value : {response.Errors}");

                }
                else
                    LogEvents($"Scale '{scaleResponse.ScaleId}' : Scale value posted successfully using Graphql call.");

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at SendScaleValue().", ex);

            }
        }
        private async Task ReadScaleOnServieStart(TudCommand scaleCommand)
        {
            try
            {
                if (!scaleCommand.openClose)
                {
                    LogEvents($"Triggerring Scale '{scaleCommand.scaleName}' on service start");
                    var scaleIdKey = scaleCommand.id;

                    IHandleScaleReader scaleReaderHandler;

                    if (!_Scales.TryGetValue(scaleIdKey, out scaleReaderHandler))
                    {
                        scaleReaderHandler = new HandleScaleReader(_handleCamera, _logger, _aPI);
                        _Scales[scaleIdKey] = scaleReaderHandler;

                        scaleReaderHandler.ProcessSignalRCommandHandler(scaleCommand, false, true);
                    }
                    else
                    {
                        scaleReaderHandler.ProcessSignalRCommandHandler(scaleCommand, false, true);
                    }


                }

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock("Exception at ReadScaleOnServieStart().", ex);
            }

        }

        public async Task CloseSignalR()
        {
            try
            {
                _cts?.Cancel(); ;

            }
            catch (Exception)
            {

            }

            try
            {
                if (_subscriptionHubClient != null && _subscriptionHubClient._connection != null)
                    await _subscriptionHubClient._connection.DisposeAsync();
            }
            catch (Exception ex)
            {

            }
        }
        private void LogEvents(string input)
        {
            _logger.LogWithNoLock($" {input}");
        }

    }
}
