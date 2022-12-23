using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class EditUniClientRequest
    {
        [JsonProperty(PropertyName = "name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "note")]
        [JsonPropertyName("note")]
        public string Note { get; set; }

        [JsonProperty(PropertyName = "usergroup_id")]
        [JsonPropertyName("usergroup_id")]
        public string UserGroupId { get; set; }

        [JsonProperty(PropertyName = "hostname")]
        [JsonPropertyName("hostname")]
        public string HostName { get; set; }
    }
}
