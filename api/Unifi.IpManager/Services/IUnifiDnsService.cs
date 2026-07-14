using System.Collections.Generic;
using System.Threading.Tasks;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;

namespace Unifi.IpManager.Services;

public interface IUnifiDnsService
{
    Task<ServiceResult<List<HostDnsRecord>>> GetHostDnsRecords();

    Task<ServiceResult<HostDnsRecord>> CreateHostDnsRecord(HostDnsRecord hostRecord);

    Task<ServiceResult<HostDnsRecord>> UpdateDnsHostRecord(HostDnsRecord hostRecord);

    Task<ServiceResult> DeleteHostDnsRecord(string id);
}
