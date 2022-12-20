using Newtonsoft.Json;

namespace unifi.ipmanager.Models.DTO
{
    public class ProvisionRequest
    {
        public string Group { get; set; }

        public string Name { get; set; }

        public string HostName { get; set; }

        [JsonProperty(PropertyName = "Static_ip")]
        public bool StaticIp { get; set; }

        [JsonProperty(PropertyName = "Sync_dns")]
        public bool SyncDns { get; set; }
    }
}
