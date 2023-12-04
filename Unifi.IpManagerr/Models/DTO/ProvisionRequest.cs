using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace unifi.ipmanager.Models.DTO
{
    public class ProvisionRequest
    {
        public string Group { get; set; }

        public string Name { get; set; }

        public string HostName { get; set; }

        [JsonProperty("Static_ip")]
        [JsonPropertyName("Static_ip")]
        public bool StaticIp { get; set; }

        [JsonPropertyName("Sync_dns")]
        [JsonProperty("Sync_dns")]
        public bool SyncDns { get; set; }
    }
}
