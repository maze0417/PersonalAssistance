using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace PunchCard.Models
{
    public class PunchCardRequest
    {
        public string cid { get; set; }
        public string pid { get; set; }
        public string deviceId { get; set; }
        public string macAddress { get; set; }
    }

    public class UserData
    {
        public long date { get; set; }
        public string weekDay { get; set; }
        public string lunarDateString { get; set; }
        public string festivalName { get; set; }
        public int workType { get; set; }
        public int handleStatus { get; set; }
        public int compareStatus { get; set; }
        public string timeStart { get; set; }
        public string timeEnd { get; set; }
        public bool overAttEnable { get; set; }
        public bool overAttRequiredReason { get; set; }
        public long overAttCardDataId { get; set; }
        public long punchCardTime { get; set; }
        public string dateString { get; set; }
        public string workTypeString { get; set; }
        public string handleStatusString { get; set; }
        public string compareStatusString { get; set; }
        public bool isWorkDay { get; set; }
    }

    public class PunchCardResponse
    {
        public bool success { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public List<UserData> data { get; set; }
        public string errorCode { get; set; }
        public DateTime punchTime { get; set; }
    }
}