using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi.Requests
{
    public class EditUniClientRequest
    {
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("note")]
        [JsonPropertyName("note")]
        public string Note { get; set; }

        [JsonProperty("usergroup_id")]
        [JsonPropertyName("usergroup_id")]
        public string UserGroupId { get; set; }

        [JsonProperty("hostname")]
        [JsonPropertyName("hostname")]
        public string HostName { get; set; }
    }
}
