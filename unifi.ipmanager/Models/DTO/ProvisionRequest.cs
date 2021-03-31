using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unifi.ipmanager.Models
{
    public class ProvisionRequest
    {
        public string Group { get; set; }

        public string Name { get; set; }

        public string HostName { get; set; }

        public bool Static_ip { get; set; }

        public bool Sync_dns { get; set; }
    }
}
