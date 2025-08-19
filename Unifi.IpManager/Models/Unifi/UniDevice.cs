using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi;

public class UniDevice
{
    [JsonProperty("_id")]
    [JsonPropertyName("_id")]
    public string Id { get; set; }

    public string Name { get; set; }

    public string Mac { get; set; }

    [JsonProperty("config_network")]
    [JsonPropertyName("config_network")]
    public UniNetworkConfig Network { get; set; }
}
