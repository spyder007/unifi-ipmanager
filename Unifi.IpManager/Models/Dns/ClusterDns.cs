using System.Collections.Generic;
using Unifi.IpManager.ExternalServices;

namespace Unifi.IpManager.Models.Dns
{
    public class ClusterDns
    {
        public string Name { get; set; }
        public string ZoneName { get; set; }
        public List<DnsRecord> ControlPlane { get; set; }

        public List<DnsRecord> Traffic { get; set; }
    }
}
