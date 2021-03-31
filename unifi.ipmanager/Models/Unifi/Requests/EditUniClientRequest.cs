using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unifi.ipmanager.Models.Unifi.Requests
{
    public class EditUniClientRequest
    {
        public string name { get; set; }

        public string note { get; set; }

        public string usergroup_id { get; set; }
    }
}
