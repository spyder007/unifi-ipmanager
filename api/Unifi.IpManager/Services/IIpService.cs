using System.Collections.Generic;
using System.Threading.Tasks;
using Unifi.IpManager.Models.Unifi;

namespace Unifi.IpManager.Services;

public interface IIpService
{
    Task<string> GetUnusedNetworkIpAddress(UnifiNetwork network, List<string> usedIps);

    Task ReturnIpAddress(string ipAddress);
}
