using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi;

public class UnifiNetwork
{
    [JsonProperty("_id")]
    [JsonPropertyName("_id")]
    public string Id { get; set; }

    [JsonProperty("site_id")]
    [JsonPropertyName("site_id")]
    public string SiteId { get; set; }

    public string Name { get; set; }
}
