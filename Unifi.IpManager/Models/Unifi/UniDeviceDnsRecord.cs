using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi;

public class UniDeviceDnsRecord
{
    [JsonProperty("hostname")]
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; }

    [JsonProperty("ip_address")]
    [JsonPropertyName("ip_address")]
    public string IpAddress { get; set; }

    [JsonProperty("mac_address")]
    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; }
}
