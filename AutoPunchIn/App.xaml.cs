using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Core;
using Core.Models.NueIp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PunchCardApp;

namespace AutoPunchIn
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }


        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton(provider => (ILoggerReader) provider.GetService<ILogger>());


            services.AddSingleton<IAppConfiguration>(s =>
            {
                if (!File.Exists("appSettings.json"))
                {
                    MessageBox.Show("miss appSettings.json", "Fatal Error");
                    Shutdown(-1);
                }

                var content = File.ReadAllText("appSettings.json");

                return new AppConfiguration(JsonSerializer.Deserialize<Setting>(content));
            });

            services.AddSingleton<IHrResourceService, HrResourceService>();
            services.AddSingleton<IPunchCardService, NueIpService>();
            services.AddSingleton<MainWindow>();
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            _serviceProvider.GetService<MainWindow>();
        }
    }
}