using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using PunchCard.Clients;

namespace PunchCard.Services
{
    internal class CustomWebHostService : WebHostService
    {
        private readonly IHrResourceClient _hrResourceClient;

        public CustomWebHostService(IWebHost host, IHrResourceClient hrResourceClient) : base(host)
        {
            _hrResourceClient = hrResourceClient;
        }

        protected override void OnStarted()
        {
            _hrResourceClient.PunchCardAsync().GetAwaiter().GetResult();
            base.OnStarted();
        }

        protected override void OnStopping()
        {
            _hrResourceClient.PunchCardAsync().GetAwaiter().GetResult();
            base.OnStopping();
        }
    }
}