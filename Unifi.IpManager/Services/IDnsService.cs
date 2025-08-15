using System.Collections.Generic;
using System.Threading.Tasks;
using Unifi.IpManager.ExternalServices;

namespace Unifi.IpManager.Services;

public interface IDnsService
{
    Task<IEnumerable<DnsRecord>> GetDnsRecordsForHostname(string hostname, string zone);

    Task<bool> AddDnsARecord(string hostname, string ip, string zone);

    Task<bool> AddDnsCNameRecord(string hostname, string alias, string zone);

    Task<bool> DeleteDnsARecord(string hostname, string ip, string zone);

    Task<bool> DeleteDnsRecord(DnsRecord dnsRecord);

    Task<bool> BulkCreateDnsRecords(IEnumerable<DnsRecord> dnsRecords);
}
