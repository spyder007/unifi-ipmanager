using System.Collections.Generic;
using System.Threading.Tasks;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;

namespace Unifi.IpManager.Services
{
    public interface IUnifiService
    {
        Task<ServiceResult<List<UniClient>>> GetAllFixedClients();

        Task<ServiceResult<UniClient>> ProvisionNewClient(string group, string name, string hostName, bool staticIp, bool syncDns);

        Task<ServiceResult<UniClient>> CreateClient(NewClientRequest editClientRequest);
        Task<ServiceResult> UpdateClient(string mac, EditClientRequest editClientRequest);

        Task<ServiceResult> DeleteClient(string mac);
    }
}
