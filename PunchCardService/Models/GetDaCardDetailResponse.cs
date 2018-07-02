using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace Core.Models
{
    public class Data
    {
        public string dateString { get; set; }
        public int status { get; set; }
        public string statusString { get; set; }
        public string timeStart { get; set; }
        public string timeEnd { get; set; }
        public List<string> cardTime { get; set; }
    }

    public class GetDaCardDetailResponse
    {
        public bool success { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public List<Data> data { get; set; }
        public string errorCode { get; set; }
    }

    public class GetDaCardDetailRequest
    {
        public string cid { get; set; }
        public string pid { get; set; }
        public string date { get; set; }
    }
}