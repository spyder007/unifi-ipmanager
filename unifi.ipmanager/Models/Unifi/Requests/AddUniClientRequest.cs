using Newtonsoft.Json;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class AddUniClientRequest
    {
        [JsonProperty(PropertyName = "mac")]
        public string Mac { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "hostname")]
        public string HostName { get; set; }

        [JsonProperty(PropertyName = "use_fixedip")]
        public bool UseFixedIp { get; set; }

        [JsonProperty(PropertyName = "network_id")]
        public string NetworkId { get; set; }

        [JsonProperty(PropertyName = "fixed_ip")]
        public string FixedIp { get; set; }

        [JsonProperty(PropertyName = "note")]
        public string Note { get; set; }

    }
}
