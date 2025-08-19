using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi;

public class UniHostRecord
{
    [JsonProperty("_id")]
    [JsonPropertyName("_id")]
    public string Id { get; set; }

    [JsonProperty("enabled")]
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonProperty("key")]
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonProperty("port")]
    [JsonPropertyName("port")]
    public int Port { get; set; } = 0;

    [JsonProperty("priority")]
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    [JsonProperty("record_type")]
    [JsonPropertyName("record_type")]
    public string RecordType { get; set; }

    [JsonProperty("ttl")]
    [JsonPropertyName("ttl")]
    public int Ttl { get; set; } = 0;

    [JsonProperty("value")]
    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonProperty("weight")]
    [JsonPropertyName("weight")]
    public int Weight { get; set; } = 0;
}
