using Spydersoft.Platform.Attributes;

namespace Unifi.IpManager.Options;

[InjectOptions(SectionName)]
public class IpOptions
{
    public const string SectionName = "IpOptions";

    public int IpCooldownMinutes { get; set; }
}
