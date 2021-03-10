using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using unifi.ipmanager.Models;

namespace unifi.ipmanager.Services
{
    public interface IUnifiService
    {
        Task<List<UniClient>> GetAllFixedClients();

        Task<UniClient> ProvisionNewClient(string group, string name, string hostName, bool staticIp);
    }
}
