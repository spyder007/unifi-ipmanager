using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class AddUniClientRequest
    {
        public string mac { get; set; }

        public string name { get; set; }

        public string hostname { get; set; }

        public bool use_fixedip { get; set; }

        public string network_id { get; set; }

        public string fixed_ip { get; set; }

        public string note { get; set; }
    }
}
