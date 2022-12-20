using Newtonsoft.Json;
using System.Collections.Generic;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class StaRequest
    {
        [JsonProperty(PropertyName = "cmd")]
        public string Cmd { get; set; }

        [JsonProperty(PropertyName = "macs")]
        public List<string> Macs { get; set; }
    }
}
