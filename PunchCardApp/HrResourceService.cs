using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Core.Clients;
using Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace PunchCardApp
{
    public interface IHrResourceService
    {
        Task<PunchCardResponse> PunchCardAsync();

        Task<List<string>> GetDayCardDetailAsync();

        DateTime[] CachedPunchTime { get; set; }

        DateTime LastMonitTime { get; set; }

        TimeSpan WorkerTime { get; }
        TimeSpan CacheInterval { get; }

        void Init();
    }

    public class HrResourceService : BaseApiClient, IHrResourceService
    {
        private const string Url = "https://pro.104.com.tw/";
        private readonly IHrResourceService _instance;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly IAppConfiguration _appConfiguration;

        public HrResourceService(ILogger logger, IAppConfiguration appConfiguration) : base(logger)
        {
            _appConfiguration = appConfiguration;
            _instance = this;
        }

        Task<PunchCardResponse> IHrResourceService.PunchCardAsync()
        {
            var content = new GetPunchCardRequest
            {
                cid = _appConfiguration.Cid,
                pid = _appConfiguration.Pid,
                deviceId = _appConfiguration.DeviceId,
                macAddress = "e0-3f-49-94-a8-60"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}hrm/psc/apis/public/punchWifiCard.action")
            {
                Content = new StringContent(JsonConvert.SerializeObject(content))
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("cookie", _appConfiguration.Cookie);
            return SendAsync<PunchCardResponse>(request);
        }

        async Task<List<string>> IHrResourceService.GetDayCardDetailAsync()
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
            var response = await SendAsync<GetDaCardDetailResponse>(request);

            return response.data.First().cardTime;
        }

        DateTime[] IHrResourceService.CachedPunchTime { get; set; }

        DateTime IHrResourceService.LastMonitTime { get; set; }
        TimeSpan IHrResourceService.WorkerTime => DateTime.Now - _instance.CachedPunchTime.First();

        TimeSpan IHrResourceService.CacheInterval =>
            _instance.CachedPunchTime.Last() - _instance.CachedPunchTime.First();

        void IHrResourceService.Init()
        {
            Task.Factory.StartNew(MonitorApiAsync, TaskCreationOptions.LongRunning);
        }

        private async Task MonitorApiAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                _instance.LastMonitTime = DateTime.Now;
                var cachePunchTime = _instance.CachedPunchTime;

                if (cachePunchTime == null
                    || cachePunchTime.Length == 0
                    || (DateTime.Now - cachePunchTime.Last()).TotalDays >= 1
                    || cachePunchTime.Length < 2
                    || _instance.CacheInterval.Hours < 9)
                {
                    await PunchCardIfNeededAndCacheDayCardAsync();
                }

                Delay(_cts.Token);
            }
        }

        private async Task PunchCardIfNeededAndCacheDayCardAsync()
        {
            var cardTime = await _instance.GetDayCardDetailAsync();
            if (cardTime.Count == 0 || _instance.WorkerTime.TotalHours >= 9)
            {
                await _instance.PunchCardAsync();
                return;
            }
            _instance.CachedPunchTime =
                cardTime.Select(a => DateTime.Parse($"{DateTime.Now:yyyy/MM/dd} {a}")).ToArray();
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