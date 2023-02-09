using System.Collections.Generic;

namespace unifi.ipmanager.Models.Dns
{
    public class NewClusterRequest
    {
        public string Name { get; set; }

        public string ZoneName { get; set; }

        public List<string> ControlPlaneIps { get; set; }

        public List<string> TrafficIps { get; set; }
    }
}
