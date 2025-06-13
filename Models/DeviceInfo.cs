using System.Text.Json.Serialization;

namespace IMUTestApp.Models
{
    public class DeviceInfo
    {
        [JsonPropertyName("product")]
        public string Product { get; set; } = string.Empty;
        
        [JsonPropertyName("fw_ver")]
        public string FwVer { get; set; } = string.Empty;
        
        [JsonPropertyName("bt_name")]
        public string BtName { get; set; } = string.Empty;
        
        [JsonPropertyName("cpu_id")]
        public string CpuId { get; set; } = string.Empty;
        
        [JsonPropertyName("ap_name")]
        public string ApName { get; set; } = string.Empty;
        
        [JsonPropertyName("ap_addr")]
        public string ApAddr { get; set; } = string.Empty;
    }
    
    public class DeviceInfoResponse
    {
        [JsonPropertyName("DevInfo")]
        public DeviceInfo DevInfo { get; set; } = new();
        
        [JsonPropertyName("res")]
        public int Result { get; set; }
    }
}