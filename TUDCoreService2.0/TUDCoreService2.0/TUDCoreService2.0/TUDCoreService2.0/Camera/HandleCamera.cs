using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TUDCoreService2._0.Models;
using TUDCoreService2._0.Utilities;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.Camera
{
    public class HandleCamera : IHandleCamera
    {
        string _workStationId;
        private readonly ITUDSettings _tudSettings;
        private readonly IConfiguration _configuration;
        private readonly ICamera _camera;
        private readonly ICameraGroup _cameraGroup;
        private readonly INLogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public HandleCamera(IConfiguration configuration, ITUDSettings tudSettings, INLogger logger, IHttpClientFactory httpClientFactory, ICamera camera, ICameraGroup cameraGroup)
        {
            _tudSettings = tudSettings;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _camera = camera;
            _cameraGroup = cameraGroup;
            _tudSettings = _configuration.GetSection("TUDSettings").Get<TUDSettings>();
        }
        public async Task TriggerCamera(JpeggerCameraCaptureRequest request, string workStationId)
        {
            try
            {
                _workStationId = workStationId;

                if (request == null) { return; }

                var cameraGroup = _cameraGroup.GetConfiguredCameraGroups(request.CameraName.Trim(), request.YardId).Result;

                if (cameraGroup?.Count() > 0)
                {
                    LogEvents($"Firing Camera group '{request.CameraName}'");
                    await HandleGroupCameras(cameraGroup, request.CaptureDataApi);
                }
                else
                {
                    var cameraInfo = _camera.GetConfiguredCamera(request.CameraName.Trim(), request.YardId).Result;

                    if (cameraInfo != null && cameraInfo.IsNetCam == 1 && !string.IsNullOrEmpty(cameraInfo.URL))
                    {
                        LogEvents($"Firing Single Camera '{request.CameraName}'");
                        await CaptureCameraImage(cameraInfo, request);
                    }
                    else if (cameraInfo != null)
                    {
                        LogEvents($"Not a valid camera to take picture or no url is provided ");
                    }
                    else
                    {
                        LogEvents($"Not a valid camera in the list. ");
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            await Task.CompletedTask;
        }


        private async Task<bool> CaptureCameraImage(ICamera camera, JpeggerCameraCaptureRequest request)
        {
            try
            {
                if (camera != null && !string.IsNullOrEmpty(camera.URL))
                {
                    await TakePicture(camera, request.CaptureDataApi);

                    if (request.CaptureDataApi.CaptureCameraPictures != null && !request.CaptureDataApi.CaptureCameraPictures.Contains(camera.camera_name))
                    {
                        request.CaptureDataApi.CameraPostSuccess.Clear();

                        return false;
                    }
                    if (request.CaptureDataApi.CameraPostSuccess.Contains(camera.camera_name))
                    {
                        request.CaptureDataApi.CameraPostSuccess.Clear();

                        return true;
                    }
                    else
                    {
                        request.CaptureDataApi.CameraPostSuccess.Clear();

                        return false;
                    }
                }
                request.CaptureDataApi.CameraPostSuccess.Clear();

                return false;


            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleCamera.CaptureCameraImage", ex);
                return false;
            }
        }

        private async Task HandleGroupCameras(List<ICamera> cameras, JpeggerCameraCaptureDataModel request)
        {
            try
            {
                var cameraResponse = new List<bool>();
                var failedCamera = new StringBuilder();
                var failedJpeggerPost = new StringBuilder();

                var takePictureTasks = new List<Task>();
                request.CameraGroupName = request.CameraName;

                foreach (var camera in cameras)
                {
                    takePictureTasks.Add(TakePicture(camera, request));
                }

                await Task.WhenAll(takePictureTasks);


                var failedCamerasList = cameras.Where(x => !request.CaptureCameraPictures.Any(y => y == x.camera_name));

                foreach (var camera in failedCamerasList)
                {
                    failedCamera.Append($"Camera: {camera.camera_name}, Ip: {camera.ip_address} \n");
                }

                if (!string.IsNullOrEmpty(failedCamera.ToString()))
                    failedCamera.Insert(0, $"Jpegger Camera Capture failed. Check configuration \n");

                var failedPostsList = request.CaptureCameraPictures.Where(x => !request.CameraPostSuccess.Any(y => y == x));

                foreach (var camera in failedPostsList)
                {
                    failedJpeggerPost.Append($"Camera: {camera} \n");
                }

                if (!string.IsNullOrEmpty(failedJpeggerPost.ToString()))
                    failedJpeggerPost.Insert(0, $"Error in posting images into jpegger API \n");

                failedCamera.Append(failedJpeggerPost.ToString());

                request.CaptureCameraPictures.Clear();
                request.CameraPostSuccess.Clear();
            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleCamera.HandleGroupCameras", ex);
            }
        }
        private async Task TakePicture(ICamera camera, JpeggerCameraCaptureDataModel request)
        {

            try
            {
                LogEvents($"Firing Camera '{camera.camera_name}' with IP '{camera.ip_address}' and Port '{camera.port_nbr}'");
                var requestUri = new Uri(camera.URL);

                var req = WebRequest.Create(camera.URL);
                if (!string.IsNullOrEmpty(camera.username)
                    && !string.IsNullOrEmpty(camera.pwd))
                    req.Credentials = new NetworkCredential(camera.username, camera.pwd);
                req.Method = "GET";

                var cts = new CancellationTokenSource();
                cts.CancelAfter(10000);

                var httpWebRequest = (HttpWebRequest)req;
                var response = await Extensions.GetResponseAsync(httpWebRequest, cts.Token);

                Stream stream = response.GetResponseStream();

                byte[] buffer = new byte[5000000];
                int read, total = 0;
                while ((read = stream.Read(
                           buffer,
                           total,
                           1000))
                       != 0)
                {
                    total += read;
                }

                var image = new MemoryStream(
                    buffer,
                    0,
                    total);

                if (image != null && image.Length > 0)
                {
                    LogEvents($"Success in Capturing image from  Camera '{camera.camera_name}'");
                    await PostJpeggerImage(image, request, camera.camera_name);
                }
                else
                {
                    LogEvents($"Failed in Capturing image from  Camera '{camera.camera_name}'");
                }
            }
            catch (Exception ex)
            {
                LogExceptionEvents($"Exception at HandleCamera.TakePicture : Camera '{camera.camera_name}' with IP '{camera.ip_address}' and Port '{camera.port_nbr}'", ex);
            }


        }

        private async Task PostJpeggerImage(Stream image, JpeggerCameraCaptureDataModel request, string cameraName)
        {
            try
            {
                var postTime = DateTime.Now;
                var count = 0;
                var retryPolicy = HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutRejectedException>()
                    .Or<Exception>()
                    .RetryAsync(
                        3,
                        async (exception, retryCount) =>
                        {
                            await Task.Delay(3000);
                            count++;

                            _logger.LogExceptionWithNoLock($"HandleCamera.cs :: PostJpeggerImage() Error :: Ticket Number ='{request.TicketNumber}', Camera Name ='{cameraName}', Camera Group Name ='{request.CameraGroupName}', Event Code ='{request.EventCode}', Previous Post Time='{postTime}', Retry :{retryCount},  Retry Time='{DateTime.Now}'", exception.Exception);
                            //Log.Logger.GetContext(typeof(Jpegger))
                            //    .Error(
                            //        exception.Exception,
                            //        $"Jpegger.cs :: PostJpeggerImage() Error :: Ticket Number ='{request.TicketNumber}', Camera Name ='{cameraName}', Camera Group Name ='{request.CameraGroupName}', Event Code ='{request.EventCode}', Previous Post Time='{postTime}', Retry :{retryCount},  Retry Time='{DateTime.Now}'");
                        });

                var retryResult = await retryPolicy.ExecuteAsync(
                    async () =>
                    {
                        var httpClient = _httpClientFactory.CreateClient("Jpegger");
                        httpClient.Timeout = TimeSpan.FromMilliseconds(30000);//

                        var formData = GenerateMultipartFormData(
                            image,
                            request,
                            cameraName);

                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));


                        if (!string.IsNullOrWhiteSpace(_tudSettings.JPEGgerToken))
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _tudSettings.JPEGgerToken);
                        postTime = DateTime.Now;
                        var response = await httpClient.PostAsync(_tudSettings.JPEGgerAPI + request.SpecifyJpeggerTable.ToLowerInvariant(), formData);

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadAsStringAsync();
                            request.CameraPostSuccess.Add(cameraName);
                            image.Dispose();
                        }
                        else
                        {
                            _logger.LogWarningWithNoLock($"HandleCamera.cs :: PostJpeggerImage() Failure Response : {response.ReasonPhrase}. Ticket Number ='{request.TicketNumber}' , Camera Name ='{cameraName}' , Camera Group Name ='{request.CameraGroupName}', Event Code ='{request.EventCode}'");
                            //Log.Logger.GetContext(typeof(Jpegger))
                            //    .Error(
                            //        response.ReasonPhrase,
                            //        $"Jpegger.cs :: PostJpeggerImage() Failure Response :: Ticket Number ='{request.TicketNumber}' , Camera Name ='{cameraName}' , Camera Group Name ='{request.CameraGroupName}', Event Code ='{request.EventCode}'");
                        }

                        return response;
                    });
            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleCamera.PostJpeggerImage", ex);
            }
        }

        private MultipartFormDataContent GenerateMultipartFormData(Stream image, JpeggerCameraCaptureDataModel request, string cameraName)
        {

            try
            {
                var table = request.SpecifyJpeggerTable.TrimEnd('s').ToLowerInvariant();
                var multipartFormContent = new MultipartFormDataContent
                {
                    { new StreamContent(image), "\"" + $"{table}[file]" + "\"", "display.jpg" },
                    { new StringContent(_tudSettings.YardId), "\"" + $"{table}[yardid]" + "\"" }
                };

                foreach (var prop in request.GetType().GetProperties())
                {
                    var propValue = prop.GetValue(request);
                    switch (prop.Name)
                    {
                        case "TicketNumber":
                            if (propValue != null && propValue.ToString() != "-1" && propValue.ToString() != "0")
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[ticket_nbr]" + "\"");
                            break;

                        case "CameraName":
                            if (cameraName != null)
                                multipartFormContent.Add(new StringContent(cameraName), "\"" + $"{table}[camera_name]" + "\"");
                            break;

                        case "CameraGroupName":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[camera_group]" + "\"");
                            break;

                        case "EventCode":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[event_code]" + "\"");
                            break;

                        case "ReceiptNumber":
                            if (propValue != null && propValue.ToString() != "-1" && propValue.ToString() != "0")
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[receipt_nbr]" + "\"");
                            break;

                        case "Location":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[location]" + "\"");
                            break;

                        case "TareSequenceNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[tare_seq_nbr]" + "\"");
                            break;

                        case "Amount":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[amount]" + "\"");
                            break;

                        case "ContractNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[contr_nbr]" + "\"");
                            break;

                        case "ContractName":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[contr_name]" + "\"");
                            break;

                        case "Weight":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[weight]" + "\"");
                            break;

                        case "CustomerName":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cust_name]" + "\"");
                            break;

                        case "CustomerNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cust_nbr]" + "\"");
                            break;

                        case "CertificationNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cert_nbr]" + "\"");
                            break;

                        case "CertificateDescription":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cert_desc]" + "\"");
                            break;

                        case "CommodityName":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[cmdy_name]" + "\"");
                            break;

                        case "ContainerNumber":
                            if (propValue != null)
                                multipartFormContent.Add(new StringContent(propValue.ToString()), "\"" + $"{table}[container_nbr]" + "\"");
                            break;
                    }
                }

                return multipartFormContent;
            }
            catch (Exception ex)
            {
                LogExceptionEvents("Exception at HandleCamera.GenerateMultipartFormData", ex);
                return null;
            }
        }

        public async Task PostMultiForm(MultipartFormDataContent content, JpeggerCameraCaptureDataModel request, string cameraName)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

                if (_tudSettings.IncludeToken == 1 && !string.IsNullOrWhiteSpace(_tudSettings.JPEGgerToken))
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _tudSettings.JPEGgerToken);


                var response = await httpClient.PostAsync(_tudSettings.JPEGgerAPI + request.SpecifyJpeggerTable.ToLowerInvariant(), content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    request.CameraPostSuccess.Add(cameraName);
                    LogEvents($"Success in posting images for Ticket Number ='{request.TicketNumber}' ,Camera '{cameraName}'");
                }
                else
                {
                    _logger.LogWarningWithNoLock($" Scale '{_workStationId}' : Warning at PostMultiForm() Failure Response : '{response.ReasonPhrase}' : Ticket Number ='{request.TicketNumber}' , Camera Name ='{cameraName}' , Camera Group Name ='{request.CameraGroupName}' ");
                }

            }
            catch (Exception ex)
            {
                LogExceptionEvents($"Exception at HandleCamera.PostMultiForm : Ticket Number ='{request.TicketNumber}' , Camera Name ='{cameraName}'", ex);
            }
        }
        private void LogEvents(string input)
        {
            _logger.LogWithNoLock($" Scale '{_workStationId}' : {input}");
        }

        private void LogExceptionEvents(string input, Exception exception)
        {
            _logger.LogExceptionWithNoLock($" Scale '{_workStationId}' : {input} :", exception);
        }
    }
}
