using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Core.Clients
{
    public abstract class BaseApiClient
    {
        private const int TimeoutInSeconds = 15;

        private readonly HttpClient _httpClient;

        private readonly ILogger _logger;

        protected BaseApiClient(ILogger logger)
        {
            _httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            });
            _logger = logger;
        }

        protected async Task<TResponse> SendWithJsonResponseAsync<TResponse>(HttpRequestMessage request)
        {
            try
            {
                using (var response = await _httpClient.SendAsync(request,
                    new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutInSeconds)).Token))
                {
                    var res = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug(res);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _logger.LogWarning(res);
                        return default;
                    }

                    return JsonConvert.DeserializeObject<TResponse>(res);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return default;
            }
        }

        protected async Task<string> SendAsync(HttpRequestMessage request, bool logResponse = true)
        {
            try
            {
                using (var response = await _httpClient.SendAsync(request,
                    new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutInSeconds)).Token))
                {
                    var res = await response.Content.ReadAsStringAsync();
                    if (logResponse)
                    {
                        _logger.LogInformation($"url : {request.RequestUri}::{res}");
                    }

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return default;
                    }

                    return res;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return default;
            }
        }
    }
}