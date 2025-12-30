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

    [JsonProperty("vlan_enabled")]
    [JsonPropertyName("vlan_enabled")]
    public bool VlanEnabled { get; set; }

    [JsonProperty("networkgroup")]
    [JsonPropertyName("networkgroup")]
    public string NetworkGroup { get; set; }

    [JsonProperty("dhcpd_start")]
    [JsonPropertyName("dhcpd_start")]
    public string DhcpStartAddress { get; set; }

    [JsonProperty("dhcpd_stop")]
    [JsonPropertyName("dhcpd_stop")]
    public string DhcpEndAddress { get; set; }

    [JsonProperty("ip_subnet")]
    [JsonPropertyName("ip_subnet")]
    public string IpSubnet { get; set; }
}
