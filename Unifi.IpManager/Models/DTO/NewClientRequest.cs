namespace Unifi.IpManager.Models.DTO
{
    public class NewClientRequest : EditClientRequest
    {
        public string MacAddress { get; set; }
        public string IpAddress { get; set; }

        public bool SyncDns { get; set; }

        public bool StaticIp { get; set; }
    }
}
