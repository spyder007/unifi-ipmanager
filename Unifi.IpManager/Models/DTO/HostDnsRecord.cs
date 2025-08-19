using Unifi.IpManager.Models.Unifi;

namespace Unifi.IpManager.Models.DTO;

public class HostDnsRecord
{
    public string Id { get; set; }

    public string Hostname { get; set; }

    public string IpAddress { get; set; }

    public string MacAddress { get; set; }

    public string RecordType { get; set; }

    public bool DeviceLock { get; set; } = false;
}
