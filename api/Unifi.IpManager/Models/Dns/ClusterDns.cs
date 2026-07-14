using System.Collections.Generic;
using Unifi.IpManager.Models.DTO;

namespace Unifi.IpManager.Models.Dns;

public class ClusterDns
{
    public string Name { get; set; }
    public string ZoneName { get; set; }
    public List<HostDnsRecord> ControlPlane { get; set; }

    public List<HostDnsRecord> Traffic { get; set; }
}
