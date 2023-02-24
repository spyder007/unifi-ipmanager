using System.Collections.Generic;

namespace unifi.ipmanager.Options
{
    public class IpOptions
    {
        public const string SectionName = "IpOptions";

        public int IpCooldownMinutes { get; set; }
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
