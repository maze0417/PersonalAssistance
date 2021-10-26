using System.Configuration;

namespace PunchCardApp
{
    public interface IAppConfiguration
    {
        string Cid { get; }
        string Pid { get; }
        string DeviceId { get; }
        string Cookie { get; }

        string NueIpCompany { get; }
        string NueIpId { get; }
        string NueIpPwd { get; }

        string Lat { get; }
        string Lng { get; }
    }

    public class AppConfiguration : IAppConfiguration
    {
        string IAppConfiguration.Cid => ConfigurationManager.AppSettings["Cid"];

        string IAppConfiguration.Pid => ConfigurationManager.AppSettings["Pid"];

        string IAppConfiguration.DeviceId => ConfigurationManager.AppSettings["DeviceId"];

        string IAppConfiguration.Cookie => ConfigurationManager.AppSettings["Cookie"];

        string IAppConfiguration.NueIpCompany => ConfigurationManager.AppSettings["NueIpCompany"];

        string IAppConfiguration.NueIpId => ConfigurationManager.AppSettings["NueIpId"];

        string IAppConfiguration.NueIpPwd => ConfigurationManager.AppSettings["NueIpPwd"];

        string IAppConfiguration.Lat => ConfigurationManager.AppSettings["lat"];

        string IAppConfiguration.Lng => ConfigurationManager.AppSettings["lng"];
    }
}