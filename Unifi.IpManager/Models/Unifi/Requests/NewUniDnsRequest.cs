using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi.Requests;

public class NewUniDnsRequest
{
    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("note")]
    [JsonPropertyName("note")]
    public string Note { get; set; }

    [JsonProperty("usergroup_id")]
    [JsonPropertyName("usergroup_id")]
    public string Record { get; set; }

    [JsonProperty("enabled")]
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
