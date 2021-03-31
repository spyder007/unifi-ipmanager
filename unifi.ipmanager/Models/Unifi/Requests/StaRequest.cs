using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class StaRequest
    {
        public string cmd { get; set; }

        public List<string> macs { get; set; }
    }
}
