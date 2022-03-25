using System.Collections.Generic;

namespace unifi.ipmanager.Models
{
    public class IpOptions
    {
        public List<IpGroup> IpGroups { get; set; }
    }

    public class IpGroup
    {
        public string Name { get; set; }

        public List<IpBlock> Blocks { get; set; }
    }

    public class IpBlock
    {
        public int Min { get; set; }

        public int Max { get; set; }
    }
}
