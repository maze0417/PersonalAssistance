using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Core.Clients;
using Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PunchCardApp;

namespace Core
{
    public class Pro104Service : BaseApiClient, IPunchCardService
    {
        private readonly IAppConfiguration _appConfiguration;
        private const string Url = "https://pro.104.com.tw/";

        public Pro104Service(IAppConfiguration appConfiguration, ILogger logger) : base(logger)
        {
            _appConfiguration = appConfiguration;
        }

        Task<PunchCardResponse> IPunchCardService.PunchCardOnWorkAsync()
        {
            var content = new GetPunchCardRequest
            {
                cid = _appConfiguration.Cid,
                pid = _appConfiguration.Pid,
                deviceId = _appConfiguration.DeviceId,
                macAddress = "b0-90-7e-a5-52-ae"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}hrm/psc/apis/public/punchWifiCard.action")
            {
                Content = new StringContent(JsonConvert.SerializeObject(content))
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("cookie", _appConfiguration.Cookie);
            return SendWithJsonResponseAsync<PunchCardResponse>(request);
        }

        Task<PunchCardResponse> IPunchCardService.PunchCardOffWorkAsync()
        {
            throw new NotImplementedException();
        }

        async Task<List<string>> IPunchCardService.GetDayCardDetailAsync()
        {
            var content = new GetDaCardDetailRequest
            {
                cid = _appConfiguration.Cid,
                pid = _appConfiguration.Pid,
                date = DateTime.Now.ToString("yyyy/MM/dd")
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}hrm/psc/apis/public/getDayCardDetail.action")
            {
                Content = new StringContent(JsonConvert.SerializeObject(content))
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("cookie", _appConfiguration.Cookie);
            var response = await SendWithJsonResponseAsync<GetDaCardDetailResponse>(request);

            return response.data.First().cardTime;
        }
    }
}