namespace Unifi.IpManager.Models.DTO
{
    public class NewClientRequest : EditClientRequest
    {
        public required string MacAddress { get; set; }
        public required string IpAddress { get; set; }

        public required bool SyncDns { get; set; }

        public required bool StaticIp { get; set; }
    }
}
