using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi.Requests;

public class AddUniClientRequest
{
    [JsonProperty("mac")]
    [JsonPropertyName("mac")]
    public string Mac { get; set; }

    [JsonProperty("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("hostname")]
    [JsonPropertyName("hostname")]
    public string HostName { get; set; }

    [JsonProperty("use_fixedip")]
    [JsonPropertyName("use_fixedip")]
    public bool UseFixedIp { get; set; }

    [JsonProperty("network_id")]
    [JsonPropertyName("network_id")]
    public string NetworkId { get; set; }

    [JsonProperty("fixed_ip")]
    [JsonPropertyName("fixed_ip")]
    public string FixedIp { get; set; }

    [JsonProperty("note")]
    [JsonPropertyName("note")]
    public string Note { get; set; }

    [JsonProperty("local_dns_record_enabled")]
    [JsonPropertyName("local_dns_record_enabled")]
    public bool UseLocalDns { get; set; } = false;

    [JsonProperty("local_dns_record")]
    [JsonPropertyName("local_dns_record")]
    public string DnsHostname { get; set; } = string.Empty;
}
