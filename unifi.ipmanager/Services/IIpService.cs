using System.Collections.Generic;
using System.Threading.Tasks;

namespace unifi.ipmanager.Services
{
    public interface IIpService
    {
        Task<string> GetUnusedGroupIpAddress(string name, List<string> usedIps);

        string GetIpGroupForAddress(string ipAddress);

        Task ReturnIpAddress(string ipAddress);
    }
}
