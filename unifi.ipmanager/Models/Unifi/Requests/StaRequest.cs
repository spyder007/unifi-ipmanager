using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class StaRequest
    {
        [JsonProperty(PropertyName = "cmd")]
        [JsonPropertyName("cmd")]
        public string Cmd { get; set; }

        [JsonProperty(PropertyName = "macs")]
        [JsonPropertyName("macs")]
        public List<string> Macs { get; set; }
    }
}
