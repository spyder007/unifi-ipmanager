using System;
using System.Threading.Tasks;
using Unifi.IpManager.Models.Dns;
using Unifi.IpManager.Models.DTO;

namespace Unifi.IpManager.Services;

public interface IClusterDnsService
{
    Task<ServiceResult<ClusterDns>> GetClusterDns(string name, string zone);

    Task<ServiceResult<ClusterDns>> CreateClusterDns(NewClusterRequest clusterDns);

    Task<ServiceResult<ClusterDns>> UpdateClusterDns(ClusterDns clusterDns);
}
