using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PunchCard.Clients;
using PunchCard.Models;

// ReSharper disable InconsistentNaming

namespace PunchCard.Services
{
    public interface IHrResourceService
    {
        Task<PunchCardResponse> PunchCardAsync();

        Task<List<string>> GetDayCardDetailAsync();

        DateTime[] CachedPunchTime { get; set; }

        DateTime LastTimerTime { get; set; }

        TimeSpan WorkerTime { get; set; }
    }

    public class HrResourceService : BaseApiClient, IHrResourceService
    {
        private const string Url = "https://pro.104.com.tw/";

        public HrResourceService(ILogger<HrResourceService> logger) : base(logger)
        {
            var instance = (IHrResourceService)this;
            new Timer(s =>
            {
                instance.LastTimerTime = DateTime.Now;
                var cachePunchTime = instance.CachedPunchTime;
                if (cachePunchTime == null || cachePunchTime.Length == 0)
                {
                    var cardTime = instance.GetDayCardDetailAsync().GetAwaiter().GetResult();
                    if (cardTime == null || cardTime.Count == 0)
                    {
                        instance.PunchCardAsync().GetAwaiter().GetResult();
                        return;
                    }

                    instance.CachedPunchTime =
                        cardTime.Select(a => DateTime.Parse($"{DateTime.Now:yyyy/MM/dd} {a}")).ToArray();
                }

                if (cachePunchTime == null)
                {
                    return;
                }
                instance.WorkerTime = DateTime.Now - cachePunchTime.First();
                if (cachePunchTime.Length >= 2)
                {
                    var total = cachePunchTime.Last() - cachePunchTime.First();
                    if (total.Hours >= 9)
                    {
                        return;
                    }
                }
                if (instance.WorkerTime.TotalHours >= 9)
                {
                    instance.PunchCardAsync().GetAwaiter().GetResult();
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
        }

        Task<PunchCardResponse> IHrResourceService.PunchCardAsync()
        {
            var content = new PunchCardRequest
            {
                cid = "24726",
                pid = "9356086",
                deviceId = "f8cbcb51a49f6e87",
                macAddress = "e0-3f-49-94-a8-60"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}hrm/psc/apis/public/punchWifiCard.action")
            {
                Content = new StringContent(JsonConvert.SerializeObject(content))
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("cookie", "BS2=undefined; CID=5fa91844ee7ee888174469f18dae49aa; PID=412885c0252a668c235d9dc87fbc70ad; proapp=1");
            return SendAsync<PunchCardResponse>(request);
        }

        async Task<List<string>> IHrResourceService.GetDayCardDetailAsync()
        {
            var content = new GetDaCardDetailRequest
            {
                cid = "24726",
                pid = "9356086",
                date = DateTime.Now.ToString("yyyy/MM/dd")
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}hrm/psc/apis/public/getDayCardDetail.action")
            {
                Content = new StringContent(JsonConvert.SerializeObject(content))
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("cookie", "BS2=undefined; CID=5fa91844ee7ee888174469f18dae49aa; PID=412885c0252a668c235d9dc87fbc70ad; proapp=1");
            var response = await SendAsync<GetDaCardDetailResponse>(request);

            return response.data.First().cardTime;
        }

        DateTime[] IHrResourceService.CachedPunchTime { get; set; }

        DateTime IHrResourceService.LastTimerTime { get; set; }
        TimeSpan IHrResourceService.WorkerTime { get; set; }
    }
}