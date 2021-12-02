using System.Collections.Generic;

namespace Core.Models.NueIp
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Section
    {
        public string s_sn { get; set; }
        public string type_code { get; set; }
        public string code { get; set; }
        public string lang_key { get; set; }
        public string code_val_tc { get; set; }
        public string code_val_sc { get; set; }
        public string code_val_en { get; set; }
        public string code_val_th { get; set; }
        public string code_val_vn { get; set; }
        public string code_val_jp { get; set; }
        public string code_val_id { get; set; }
        public string code_val2_tc { get; set; }
        public string code_val2_sc { get; set; }
        public string code_val2_en { get; set; }
        public string code_val2_th { get; set; }
        public string code_val2_vn { get; set; }
        public string code_val2_jp { get; set; }
        public string code_val2_id { get; set; }
        public string remark { get; set; }
        public string rec_status { get; set; }
        public string sort { get; set; }
    }

    public class User
    {
        public string u_no { get; set; }
        public string u_sn { get; set; }
        public string A1 { get; set; }
        public string IDA1 { get; set; }
        public string WWA1 { get; set; }
        public string STATUSA1 { get; set; }
        public string remarkA1 { get; set; }
        public string A2 { get; set; }
        public string IDA2 { get; set; }
        public string WWA2 { get; set; }
        public string STATUSA2 { get; set; }
        public string remarkA2 { get; set; }
        public string day { get; set; }
    }

    public class GetClockResponse
    {
        public string status { get; set; }
        public bool display { get; set; }
        public bool flag { get; set; }
        public string gps_rule { get; set; }
        public bool enabled_gps { get; set; }
        public bool enabled_ip { get; set; }
        public string zone { get; set; }
        public List<Section> section { get; set; }
        public User user { get; set; }
        public string emout { get; set; }
    }

 
 
}