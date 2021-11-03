using System;
using System.Configuration;
using System.IO;
using System.Text.Json;
using System.Windows;
using Core.Models.NueIp;
using PunchCardApp;

namespace AutoPunchIn
{
    public sealed class AppConfiguration : IAppConfiguration
    {
        private readonly Setting _setting;

        public AppConfiguration(Setting setting)
        {
            _setting = setting;
        }


        string IAppConfiguration.Cid => throw new NotImplementedException();

        string IAppConfiguration.Pid => throw new NotImplementedException();

        string IAppConfiguration.DeviceId => throw new NotImplementedException();

        string IAppConfiguration.Cookie => throw new NotImplementedException();

        string IAppConfiguration.NueIpCompany => _setting.NueIpCompany;

        string IAppConfiguration.NueIpId => _setting?.NueIpId;

        string IAppConfiguration.NueIpPwd => _setting?.NueIpPwd;

        string IAppConfiguration.Lat => _setting?.Lat;

        string IAppConfiguration.Lng => _setting?.Lng;
    }
}