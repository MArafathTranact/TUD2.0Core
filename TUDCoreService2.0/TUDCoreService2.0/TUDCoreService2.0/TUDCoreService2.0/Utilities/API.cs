using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Formatting;
using TUDCoreService2._0.Utilities.Interface;

namespace TUDCoreService2._0.Utilities
{
    public class API : IAPI
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ITUDSettings _tUDSettings;
        private readonly INLogger _logger;
        public API(ITUDSettings tUDSettings, IConfiguration configuration, IHttpClientFactory httpClientFactory, INLogger logger)
        {
            _tUDSettings = tUDSettings;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _tUDSettings = _configuration.GetSection("TUDSettings").Get<TUDSettings>();
        }
        public async Task<T> GetRequest<T>(string param)
        {
            string responseBody = string.Empty;
            var method = "";
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _tUDSettings.JPEGgerToken);
                //client.Timeout = TimeSpan.FromSeconds(APITimeOut);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                method = _tUDSettings.JPEGgerAPI + param;
                using (HttpResponseMessage response = client.GetAsync(method).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        responseBody = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        _logger.LogWarningWithNoLock($" Failure code : {response.ReasonPhrase} , Method url : {method}");

                    }
                }

            }
            catch (HttpRequestException ex)
            {
                _logger.LogExceptionWithNoLock($" Method url : {method}", ex);
                return default;

            }
            catch (TaskCanceledException ex)
            {
                _logger.LogExceptionWithNoLock($" Method url : {method}", ex);
                return default;

            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Method url : {method}", ex);
                return default;

            }
            var options = new JsonSerializerOptions { IncludeFields = true };


            return string.IsNullOrEmpty(responseBody) ? default : JsonSerializer.Deserialize<T>(responseBody, options);
        }

        public async Task<T> PutRequest<T>(T updateItem, string param)
        {
            string responseBody = string.Empty;
            var method = "";

            try
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _tUDSettings.JPEGgerToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                method = _tUDSettings.JPEGgerAPI + param;
                _logger.LogWithNoLock($"Update Url : {method}");
                using (HttpResponseMessage response = client.PutAsync(method, updateItem, new JsonMediaTypeFormatter()).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        responseBody = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        _logger.LogWarningWithNoLock($" Failure code : {response.ReasonPhrase} , Method url : {method}");
                    }
                }

            }
            catch (HttpRequestException ex)
            {
                _logger.LogExceptionWithNoLock($" Method url : {method}", ex);
                return default;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogExceptionWithNoLock($" Method url : {method}", ex);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogExceptionWithNoLock($" Method url : {method}", ex);
                return default;
            }

            return string.IsNullOrEmpty(responseBody) ? default : JsonSerializer.Deserialize<T>(responseBody);
        }
    }
}
