namespace unifi.ipmanager.Options
{
    public class DnsServiceOptions
    {
        public const string SectionName = "DnsService";

        public string Url { get; set; }

        public string DefaultZone { get; set; } = string.Empty;
    }
}
