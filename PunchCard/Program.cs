using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PunchCard.Clients;
using PunchCard.Services;

namespace PunchCard
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var pathToContentRoot = @"C:\Users\MazeChang\Source\Repos\PersonalAssistance\PunchCard";

            var webHostArgs = args.Where(arg => arg != "--console").ToArray();

            var host = WebHost.CreateDefaultBuilder(webHostArgs)
                .UseContentRoot(pathToContentRoot)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://localhost:5050")
                .Build();

            if (isService)
            {
                //host.RunAsCustomService();
                host.RunAsService();
            }
            else
            {
                host.Run();
                Console.WriteLine("Run As Console");
            }
        }
    }

    public static class WebHostServiceExtensions
    {
        public static void RunAsCustomService(this IWebHost host)
        {
            var webHostService = new CustomWebHostService(host, host.Services.GetService<IHrResourceClient>());
            ServiceBase.Run(webHostService);
        }
    }
}