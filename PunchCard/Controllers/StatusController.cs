using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PunchCard.Services;

// ReSharper disable InconsistentNaming

namespace PunchCard.Controllers
{
    public class StatusController : Controller
    {
        private readonly IHrResourceService _hrResourceService;

        public StatusController(IHrResourceService hrResourceService)
        {
            _hrResourceService = hrResourceService;
        }

        [Route("status"), HttpGet]
        public async Task<ServiceStatus> GetServerStatusAsync()
        {
            return new ServiceStatus
            {
                user_name = Environment.UserName,
                user_domain_name = Environment.UserDomainName,
                processor_count = Environment.ProcessorCount,
                is_environment_user_interactive = Environment.UserInteractive,
                current_server_time = DateTimeOffset.Now,
                server_name = Environment.MachineName,
                location = AppDomain.CurrentDomain.BaseDirectory,
                version = GetType().Assembly.GetName().Version.ToString(),
                punch_responses = _hrResourceService.CachedPunchTime,
                last_timer_time = _hrResourceService.LastTimerTime.ToString(CultureInfo.InvariantCulture),
                work_time = _hrResourceService.WorkerTime.ToString(@"hh\:mm\:ss"),
                card_time = await _hrResourceService.GetDayCardDetailAsync()
            };
        }

        public class ServiceStatus
        {
            public string user_name { get; set; }
            public string user_domain_name { get; set; }
            public int processor_count { get; set; }
            public bool is_environment_user_interactive { get; set; }
            public DateTimeOffset current_server_time { get; set; }
            public string server_name { get; set; }
            public string location { get; set; }
            public string version { get; set; }
            public DateTime[] punch_responses { get; set; }
            public string last_timer_time { get; set; }
            public string work_time { get; set; }

            public List<string> card_time { get; set; }
        }
    }
}