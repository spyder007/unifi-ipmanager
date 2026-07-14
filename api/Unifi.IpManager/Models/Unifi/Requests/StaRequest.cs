using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi.Requests;

public class StaRequest
{
    [JsonProperty("cmd")]
    [JsonPropertyName("cmd")]
    public string Cmd { get; set; }

    [JsonProperty("macs")]
    [JsonPropertyName("macs")]
    public List<string> Macs { get; set; }
}
