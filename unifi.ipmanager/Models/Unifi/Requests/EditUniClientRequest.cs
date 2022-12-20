using Newtonsoft.Json;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class EditUniClientRequest
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "note")]
        public string Note { get; set; }

        [JsonProperty(PropertyName = "usergroup_id")]
        public string UserGroupId { get; set; }

        [JsonProperty(PropertyName = "hostname")]
        public string HostName { get; set; }
    }
}
