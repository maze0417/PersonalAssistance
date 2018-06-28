using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PunchCard.Clients;

namespace PunchCard.Controllers
{
    public class StatusController : Controller
    {
        private readonly IHrResourceClient _hrResourceClient;

        public StatusController(IHrResourceClient hrResourceClient)
        {
            _hrResourceClient = hrResourceClient;
        }

        [Route("status"), HttpGet]
        public ServiceStatus GetServerStatus()
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
                LogResponses = _hrResourceClient.GetServiceCallLogs()
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
            public PunchCardResponse[] LogResponses { get; set; }
        }
    }
}