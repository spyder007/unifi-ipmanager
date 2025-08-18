using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;
using UnifiRequests = Unifi.IpManager.Models.Unifi.Requests;
using Unifi.IpManager.Options;
using Spydersoft.Platform.Attributes;


namespace Unifi.IpManager.Services;

[DependencyInjection(typeof(IUnifiService), LifetimeOfService.Scoped)]
public class UnifiService(
    IOptions<UnifiControllerOptions> options,
    IIpService ipService,
    IUnifiClient unifiClient) : IUnifiService
{
    private IIpService IpService { get; } = ipService;

    private UnifiControllerOptions UnifiOptions { get; } = options.Value;

    #region IUnifiService Implementation

    public async Task<ServiceResult<List<UnifiNetwork>>> GetAllNetworks()
    {
        return await unifiClient.ExecuteRequest<List<UnifiNetwork>>(
            unifiClient.BaseApiUrlV1.AppendPathSegments(unifiClient.SiteId, "rest", "networkconf"),
            async (request) =>
            {
                return await request.GetJsonAsync<UniResponse<List<UnifiNetwork>>>();
            });
    }

    public async Task<ServiceResult<List<UniClient>>> GetAllFixedClients()
    {
        var result = new ServiceResult<List<UniClient>>();

        var allClients = new List<UniClient>();

        try
        {
            var fixedClients = await GetAllFixedIpClients();
            allClients.AddRange(fixedClients);
        }
        catch (Exception e)
        {
            result.MarkFailed(e);
        }

        try
        {
            var devices = await GetDevicesAsUniClient();
            if (devices.Success)
            {
                if (devices.Data != null && devices.Data.Count != 0)
                {
                    allClients.AddRange(devices.Data);
                }
            }
            else
            {
                result.MarkFailed(devices.Errors);
                return result;
            }
        }
        catch (Exception e)
        {
            result.MarkFailed(e);
            return result;
        }

        result.MarkSuccessful(allClients);

        return result;
    }

    public async Task<ServiceResult<UniClient>> CreateClient(NewClientRequest editClientRequest)
    {
        var result = new ServiceResult<UniClient>();

        var clientExistsResult = await ClientExists(editClientRequest.MacAddress);

        if (!clientExistsResult.Success)
        {
            result.MarkFailed("Create Failed");
            return result;
        }

        if (clientExistsResult.Data)
        {
            result.MarkFailed($"Client with Mac Address {editClientRequest.MacAddress} already exists.");
        }

        var note = new UniNote()
        {
            DnsHostname = editClientRequest.Hostname,
            SetOnDevice = false,
            SyncDnsHostName = editClientRequest.SyncDns
        };

        var noteString = JsonConvert.SerializeObject(note, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        var networkId = await GetNetworkId(editClientRequest.Network);

        if (string.IsNullOrWhiteSpace(networkId))
        {
            result.MarkFailed($"Network could not be found. {editClientRequest.Network}");
            return result;
        }

        var addRequest = new UnifiRequests.AddUniClientRequest
        {
            Mac = editClientRequest.MacAddress,
            Name = editClientRequest.Name,
            HostName = editClientRequest.Hostname,
            UseFixedIp = true,
            FixedIp = editClientRequest.IpAddress,
            NetworkId = networkId,
            Note = noteString,
            UseLocalDns = true,
            DnsHostname = $"{editClientRequest.Hostname}.{UnifiOptions.DnsZone}"
        };

        return await ExecuteAddUniClientRequest(addRequest);
    }

    public async Task<ServiceResult> UpdateClient(string mac, EditClientRequest editClientRequest)
    {
        var result = new ServiceResult();

        var clientResult = await GetClient(mac);

        if (!clientResult.Success)
        {
            result.MarkFailed("Update Failed");
            return result;
        }

        var editRequest = new UnifiRequests.EditUniClientRequest()
        {
            Name = editClientRequest.Name,
            HostName = editClientRequest.Hostname
        };

        var updateNotes = new UniNote();
        // If the current object has notes, update with incoming.
        if (clientResult.Data.Notes != null)
        {
            clientResult.Data.Notes.Update(editClientRequest.Notes);
            updateNotes = clientResult.Data.Notes;
        }
        else if (editClientRequest.Notes != null)
        {
            updateNotes = editClientRequest.Notes;
        }

        editRequest.Note = JsonConvert.SerializeObject(updateNotes, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

        var postRequestString = JsonConvert.SerializeObject(editRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

        return await unifiClient.ExecuteRequest(
            unifiClient.BaseApiUrlV1.AppendPathSegments(unifiClient.SiteId, "rest", "user", clientResult.Data.Id),
            async (request) =>
            {
                return await request.PutStringAsync(postRequestString)
                    .ReceiveJson<UniResponse<List<UniClient>>>();


            }, true);
    }

    public async Task<ServiceResult<UniClient>> ProvisionNewClient(ProvisionRequest request)
    {
        var result = new ServiceResult<UniClient>();

        var clientsResult = await GetAllFixedClients();
        if (!clientsResult.Success)
        {
            result.MarkFailed(clientsResult.Errors);
        }

        var clients = clientsResult.Data;
        var macAddress = string.Empty;

        do
        {
            macAddress = GenerateMacAddress();
        } while (clients.Exists(c => c.Mac == macAddress));

        var note = new UniNote()
        {
            DnsHostname = request.HostName,
            SetOnDevice = false,
            SyncDnsHostName = request.SyncDns
        };

        var noteString = JsonConvert.SerializeObject(note, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        var networkId = await GetNetworkId(request.Network);

        if (string.IsNullOrWhiteSpace(networkId))
        {
            result.MarkFailed($"Network could not be found. {request.Network}");
            return result;
        }

        var addRequest = new UnifiRequests.AddUniClientRequest
        {
            Mac = macAddress,
            Name = request.Name,
            HostName = request.HostName,
            UseFixedIp = request.StaticIp,
            NetworkId = networkId,
            Note = noteString,
            UseLocalDns = request.StaticIp,
            DnsHostname = $"{request.HostName}.{UnifiOptions.DnsZone}",
        };

        if (request.StaticIp)
        {
            var assignedIp = await IpService.GetUnusedGroupIpAddress(request.Group, clients.Select(c => c.FixedIp).ToList());
            if (!string.IsNullOrWhiteSpace(assignedIp))
            {
                addRequest.FixedIp = assignedIp;
            }
        }

        return await ExecuteAddUniClientRequest(addRequest);
    }

    public async Task<ServiceResult> DeleteClient(string mac)
    {
        var clientResult = await GetClient(mac);
        if (clientResult.Success && clientResult.Data.Noted && clientResult.Data.UseFixedIp)
        {
            await IpService.ReturnIpAddress(clientResult.Data.FixedIp);
        }

        var postRequest = new UnifiRequests.StaRequest()
        {
            Cmd = "forget-sta",
            Macs = [mac]
        };

        var postRequestString = JsonConvert.SerializeObject(postRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

        return await unifiClient.ExecuteRequest(
            unifiClient.BaseApiUrlV1.AppendPathSegments(unifiClient.SiteId, "cmd", "stamgr"),
            async (request) =>
            {
                // POST https://unifi.gerega.net/proxy/network/v1/api/site/default/cmd/stamgr
                // Body: {"cmd":"forget-sta","macs":["68:a0:98:ac:e2:50"]}
                return await request
                    .PostStringAsync(postRequestString)
                    .ReceiveJson<UniResponse<List<UniClient>>>();

            }, true);
    }

    #endregion IUnifiService Implementation


    #region Private Methods

    private async Task<List<UniClient>> GetAllFixedIpClients()
    {
        var data = await unifiClient.ExecuteRequest<List<UniClient>>(
            unifiClient.BaseApiUrlV1.AppendPathSegments(unifiClient.SiteId, "rest", "user"),
            async (request) =>
            {
                return await request.GetJsonAsync<UniResponse<List<UniClient>>>();
            });

        if (!data.Success)
        {
            return new List<UniClient>();
        }
        else
        {
            data.Data.ForEach(client =>
            {
                if (string.IsNullOrWhiteSpace(client.Name))
                {
                    client.Name = client.Hostname;
                }

                if (client.UseFixedIp)
                {
                    client.IpGroup = IpService.GetIpGroupForAddress(client.FixedIp);
                }
            });
            return data.Data.Where(uc => uc.UseFixedIp).ToList();
        }
    }

    private async Task<ServiceResult<UniClient>> ExecuteAddUniClientRequest(UnifiRequests.AddUniClientRequest addRequest)
    {
        return await unifiClient.ExecuteRequest(
                    unifiClient.BaseApiUrlV1.AppendPathSegments(unifiClient.SiteId, "rest", "user"),
                    async (request) =>
                    {
                        var addRequestString = JsonConvert.SerializeObject(addRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                        var addResult = await request
                            .PostStringAsync(addRequestString)
                            .ReceiveJson<UniResponse<List<UniClient>>>();

                        return addResult.Data[0];
                    }
                    , true);
    }
    private async Task<ServiceResult<bool>> ClientExists(string mac)
    {
        return await unifiClient.ExecuteRequest<bool>(
            unifiClient.BaseApiUrlV1.AppendPathSegments(unifiClient.SiteId, "rest", "user").SetQueryParam("mac", mac),
            async (request) =>
            {
                var data = await request.GetJsonAsync<UniResponse<List<UniClient>>>();
                return data.Data.Count != 0;
            });
    }
    private async Task<ServiceResult<UniClient>> GetClient(string mac)
    {
        var data = await unifiClient.ExecuteRequest<List<UniClient>>(
            unifiClient.BaseApiUrlV1.AppendPathSegments(unifiClient.SiteId, "rest", "user").SetQueryParam("mac", mac),
            async (request) =>
            {
                return await request.GetJsonAsync<UniResponse<List<UniClient>>>();
            });
        var result = new ServiceResult<UniClient>();

        if (data.Data.Count == 0)
        {
            result.MarkFailed("Client not found.");
        }
        else
        {
            data.Data.ForEach(client =>
            {
                if (string.IsNullOrWhiteSpace(client.Name))
                {
                    client.Name = client.Hostname;
                }
                if (client.UseFixedIp)
                {
                    client.IpGroup = IpService.GetIpGroupForAddress(client.FixedIp);
                }
            });
            result.MarkSuccessful(data.Data[0]);
        }

        return result;
    }

    private async Task<ServiceResult<List<UniClient>>> GetDevicesAsUniClient()
    {
        var result = new ServiceResult<List<UniClient>>();

        try
        {

            var devicesClients = new List<UniClient>();
            var data = await unifiClient.ExecuteRequest(
                unifiClient.BaseApiUrlV1.AppendPathSegments(unifiClient.SiteId, "stat", "device"),
                async (request) =>
                {
                    return await request.GetJsonAsync<UniResponse<List<UniDevice>>>();
                });

            foreach (var client in data.Data)
            {
                devicesClients.Add(new UniClient
                {
                    Id = client.Id,
                    FixedIp = client.Network.Ip,
                    Name = client.Name,
                    Mac = client.Mac,
                    UseFixedIp = true,
                    ObjectType = "device",
                    IpGroup = IpService.GetIpGroupForAddress(client.Network.Ip)
                });
            }
            result.MarkSuccessful(devicesClients);

        }
        catch (Exception e)
        {
            result.MarkFailed(e);
        }

        return result;
    }

    private async Task<string> GetNetworkId(string networkName)
    {
        var networks = await GetAllNetworks();
        if (networks.Success)
        {
            UnifiNetwork network = networks.Data.Find(n => n.Name == networkName);
            if (network != null)
            {
                return network.Id;
            }
        }
        return null;
    }

    #endregion Private Methods

    #region Static Helpers
    private static string GenerateMacAddress()
    {
        var sBuilder = new StringBuilder();
        var r = new Random();
        _ = sBuilder.Append("00:15:5D:");
        for (int i = 0; i < 3; i++)
        {
            var number = r.Next(0, 255);
            _ = sBuilder.Append(number.ToString("X2"));
            if (i < 2)
            {
                _ = sBuilder.Append(':');
            }
        }
        return sBuilder.ToString().ToUpper();
    }

    #endregion Static Helpers
}
