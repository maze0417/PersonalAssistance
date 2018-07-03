using System.Configuration;

namespace PunchCardApp
{
    public interface IAppConfiguration
    {
        string Cid { get; }
        string Pid { get; }
        string DeviceId { get; }
        string Cookie { get; }
    }

    public class AppConfiguration : IAppConfiguration
    {
        string IAppConfiguration.Cid => ConfigurationManager.AppSettings["Cid"];

        string IAppConfiguration.Pid => ConfigurationManager.AppSettings["Pid"];

        string IAppConfiguration.DeviceId => ConfigurationManager.AppSettings["DeviceId"];

        string IAppConfiguration.Cookie => ConfigurationManager.AppSettings["Cookie"];
    }
}