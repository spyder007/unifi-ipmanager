using Unifi.IpManager.Models.Unifi;

namespace Unifi.IpManager.Models.DTO;

public static class HostDnsRecordExtensions
{
    public static UniHostRecord ToUniHostRecord(this HostDnsRecord record)
    {
        return new UniHostRecord
        {
            Id = record.Id,
            Key = record.Hostname,
            Value = record.IpAddress,
            RecordType = record.RecordType,
            Enabled = true, // Assuming enabled by default
            Port = 0, // Default port
            Priority = 0, // Default priority
            Ttl = 0, // Default TTL
            Weight = 0 // Default weight
        };
    }

    public static HostDnsRecord ToHostDnsRecord(this UniHostRecord record)
    {
        return new HostDnsRecord
        {
            Id = record.Id,
            Hostname = record.Key,
            IpAddress = record.Value,
            RecordType = record.RecordType,
            DeviceLock = false // Host records are not device locked
        };
    }

    public static HostDnsRecord ToHostDnsRecord(this UniDeviceDnsRecord record)
    {
        return new HostDnsRecord
        {
            Hostname = record.Hostname,
            IpAddress = record.IpAddress,
            MacAddress = record.MacAddress,
            RecordType = "A", // Assuming A record type for device DNS records
            DeviceLock = true // Device DNS records are device locked
        };
    }

}