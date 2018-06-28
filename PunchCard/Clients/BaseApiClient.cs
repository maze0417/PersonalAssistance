﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PunchCard.Clients
{
    public abstract class BaseApiClient
    {
        private const int TimeoutInSeconds = 15;
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly ILogger _logger;

        protected BaseApiClient(ILogger logger)
        {
            _logger = logger;
        }

        protected async Task<TResponse> SendAsync<TResponse>(HttpRequestMessage request)
        {
            try
            {
                using (var response = await HttpClient.SendAsync(request, new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutInSeconds)).Token))
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
    }
}