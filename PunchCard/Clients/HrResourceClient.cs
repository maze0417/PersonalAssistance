using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace PunchCard.Clients
{
    public interface IHrResourceClient
    {
        Task<PunchCardResponse> PunchCardAsync();

        PunchCardResponse[] GetServiceCallLogs();
    }

    public class HrResourceClient : BaseApiClient, IHrResourceClient
    {
        private const string Url = "https://pro.104.com.tw/";
        private static readonly ConcurrentQueue<PunchCardResponse> MemoryLog = new ConcurrentQueue<PunchCardResponse>();

        public HrResourceClient(ILogger<HrResourceClient> logger) : base(logger)
        {
        }

        async Task<PunchCardResponse> IHrResourceClient.PunchCardAsync()
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
            var response = await SendAsync<PunchCardResponse>(request);
            MemoryLog.Enqueue(response);
            while (MemoryLog.Count > 50)
            {
                MemoryLog.TryDequeue(out _);
            }
            return response;
        }

        PunchCardResponse[] IHrResourceClient.GetServiceCallLogs()
        {
            return MemoryLog.Reverse().ToArray();
        }
    }

    public class PunchCardRequest
    {
        public string cid { get; set; }
        public string pid { get; set; }
        public string deviceId { get; set; }
        public string macAddress { get; set; }
    }

    public class UserData
    {
        public long date { get; set; }
        public string weekDay { get; set; }
        public string lunarDateString { get; set; }
        public string festivalName { get; set; }
        public int workType { get; set; }
        public int handleStatus { get; set; }
        public int compareStatus { get; set; }
        public string timeStart { get; set; }
        public string timeEnd { get; set; }
        public bool overAttEnable { get; set; }
        public bool overAttRequiredReason { get; set; }
        public long overAttCardDataId { get; set; }
        public long punchCardTime { get; set; }
        public string dateString { get; set; }
        public string workTypeString { get; set; }
        public string handleStatusString { get; set; }
        public string compareStatusString { get; set; }
        public bool isWorkDay { get; set; }
    }

    public class PunchCardResponse
    {
        public bool success { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public List<UserData> data { get; set; }
        public string errorCode { get; set; }
    }
}