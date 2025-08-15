using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi;

public class UniHostRecord
{
    public string Id { get; set; }

    public bool Enabled { get; set; }

    [JsonProperty("key")]
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonProperty("port")]
    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonProperty("priority")]
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonProperty("record_type")]
    [JsonPropertyName("record_type")]
    public string RecordType { get; set; }

    [JsonProperty("ttl")]
    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }

    [JsonProperty("value")]
    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonProperty("weight")]
    [JsonPropertyName("weight")]
    public int Weight { get; set; }
}
