using System.Collections.Generic;

namespace unifi.ipmanager.Services
{
    public interface IIpService
    {
        string GetUnusedGroupIpAddress(string name, List<string> usedIps);

        string GetIpGroupForAddress(string ipAddress);
    }
}
