using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class AddUniClientRequest
    {
        [JsonProperty(PropertyName = "mac")]
        [JsonPropertyName("mac")]
        public string Mac { get; set; }

        [JsonProperty(PropertyName = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "hostname")]
        [JsonPropertyName("hostname")]
        public string HostName { get; set; }

        [JsonProperty(PropertyName = "use_fixedip")]
        [JsonPropertyName("use_fixedip")]
        public bool UseFixedIp { get; set; }

        [JsonProperty(PropertyName = "network_id")]
        [JsonPropertyName("network_id")]
        public string NetworkId { get; set; }

        [JsonProperty(PropertyName = "fixed_ip")]
        [JsonPropertyName("fixed_ip")]
        public string FixedIp { get; set; }

        [JsonProperty(PropertyName = "note")]
        [JsonPropertyName("note")]
        public string Note { get; set; }

    }
}
