using System;

namespace Core.Models.NueIp
{
    public class TimeClockRequest
    {
        public string action => "add";
        public string id { get; set; }
        public string attendance_time => DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        public string token { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class TimeClockResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public string datetime { get; set; }
        public string time { get; set; }
        public string rulesn { get; set; }
        public bool display_view { get; set; }
        public bool display_overtime { get; set; }
    }
}