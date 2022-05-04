using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using unifi.ipmanager.Models;
using unifi.ipmanager.Models.DTO;
using unifi.ipmanager.Models.Unifi;

namespace unifi.ipmanager.Services
{
    public interface IUnifiService
    {
        Task<ServiceResult<List<UniClient>>> GetAllFixedClients();

        Task<ServiceResult<UniClient>> ProvisionNewClient(string group, string name, string hostName, bool staticIp, bool syncDns);

        Task<ServiceResult> UpdateClient(string mac, EditClientRequest editClientRequest);

        Task<ServiceResult> DeleteClient(string mac);
    }
}
