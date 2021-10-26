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
}