using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi;

public class UniNote
{
    [JsonProperty("set_on_device")]
    [JsonPropertyName("set_on_device")]
    public bool? SetOnDevice { get; set; }

    [JsonProperty("dns_hostname")]
    [JsonPropertyName("dns_hostname")]
    public string DnsHostname { get; set; }

    public void Update(UniNote notes)
    {
        if (notes?.SetOnDevice != null)
        {
            SetOnDevice = notes.SetOnDevice;
        }

        if (notes?.DnsHostname != null)
        {
            DnsHostname = notes.DnsHostname;
        }
    }
}
