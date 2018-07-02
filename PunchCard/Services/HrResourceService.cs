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

        void Init();
    }

    public class HrResourceService : BaseApiClient, IHrResourceService
    {
        private const string Url = "https://pro.104.com.tw/";
        private readonly IHrResourceService _instance;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public HrResourceService(ILogger<HrResourceService> logger) : base(logger)
        {
            _instance = this;
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

        void IHrResourceService.Init()
        {
            Task.Factory.StartNew(MonitorApi, TaskCreationOptions.LongRunning);
        }

        private void MonitorApi()
        {
            while (!_cts.IsCancellationRequested)
            {
                _instance.LastTimerTime = DateTime.Now;
                var cachePunchTime = _instance.CachedPunchTime;

                if (cachePunchTime == null || cachePunchTime.Length == 0 || (DateTime.Now - cachePunchTime.Last()).TotalDays >= 1)
                {
                    var cardTime = _instance.GetDayCardDetailAsync().GetAwaiter().GetResult();

                    if (cardTime.Count == 0)
                    {
                        _instance.PunchCardAsync().GetAwaiter().GetResult();
                        continue;
                    }
                    _instance.CachedPunchTime =
                        cardTime.Select(a => DateTime.Parse($"{DateTime.Now:yyyy/MM/dd} {a}")).ToArray();
                }

                if (cachePunchTime == null)
                {
                    continue;
                }
                _instance.WorkerTime = DateTime.Now - cachePunchTime.First();
                if (cachePunchTime.Length >= 2)
                {
                    var total = cachePunchTime.Last() - cachePunchTime.First();
                    if (total.Hours >= 9)
                    {
                        continue;
                    }
                }
                if (_instance.WorkerTime.TotalHours >= 9)
                {
                    _instance.PunchCardAsync().GetAwaiter().GetResult();
                }
                Delay(_cts.Token);
            }
        }

        private static void Delay(CancellationToken token)
        {
            WaitASecondOrThrowIfCanceled(token);
        }

        private static void WaitASecondOrThrowIfCanceled(CancellationToken token)
        {
            token.WaitHandle.WaitOne(TimeSpan.FromMinutes(1));
            token.ThrowIfCancellationRequested();
        }
    }
}