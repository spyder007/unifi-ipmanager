using Flurl;
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
using System.IdentityModel.Tokens.Jwt;
using Unifi.IpManager.ExternalServices;


namespace Unifi.IpManager.Services;

public class UnifiService(
    IOptions<UnifiControllerOptions> options,
    IIpService ipService,
    ILogger<UnifiService> logger,
    IDnsService dnsService) : UnifiBaseService(options, logger), IUnifiService
{
    private IIpService IpService { get; } = ipService;
    private IDnsService DnsService { get; } = dnsService;

    #region IUnifiService Implementation

    public async Task<ServiceResult<List<UnifiNetwork>>> GetAllNetworks()
    {
        var result = new ServiceResult<List<UnifiNetwork>>();
        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }
        try
        {
            var data = await BaseSiteApiUrl.AppendPathSegments(SiteId, "rest", "networkconf")
                .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UnifiNetwork>>>();
            if (data.Meta.Rc == UniMeta.ErrorResponse)
            {
                Logger.LogError("Error retrieving networks from {Url}: {Message}", UnifiOptions.Url, data.Meta.Msg);
                result.MarkFailed($"Error retrieving networks from {UnifiOptions.Url}: {data.Meta.Msg}");
            }
            else
            {
                result.MarkSuccessful(data.Data);
            }
        }
        catch (Exception e)
        {
            result.MarkFailed(e);
        }
        return result;
    }

    public async Task<ServiceResult<List<UniClient>>> GetAllFixedClients()
    {
        var result = new ServiceResult<List<UniClient>>();

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

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

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

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

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

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

        var csrfToken = GetCsrfToken();

        if (!string.IsNullOrEmpty(csrfToken))
        {
            Logger.LogDebug("CSRF = {Csrf}", csrfToken);
            Logger.LogDebug("Payload String = {Payload}", postRequestString);
            try
            {
                var noteResult = await BaseSiteApiUrl
                    .AppendPathSegments(SiteId, "rest", "user", clientResult.Data.Id)
                    .WithCookies(_cookieJar)
                    .WithHeader("X-Csrf-Token", csrfToken)
                    .PutStringAsync(postRequestString)
                    .ReceiveJson<UniResponse<List<UniClient>>>();

                if (noteResult.Meta.Rc == UniMeta.ErrorResponse)
                {
                    Logger.LogError("Error updating client to {Url}: {Message}", UnifiOptions.Url, noteResult.Meta.Msg);
                    result.MarkFailed($"Error updating client to {UnifiOptions.Url}: {noteResult.Meta.Msg}");
                }
                else
                {
                    result.MarkSuccessful();
                }
            }
            catch (Exception e)
            {
                result.MarkFailed(e);
            }
        }
        else
        {
            Logger.LogDebug("CSRF Token is null");
            result.MarkFailed("No CSRF Token Present");
        }

        return result;
    }

    public async Task<ServiceResult<UniClient>> ProvisionNewClient(ProvisionRequest request)
    {
        var result = new ServiceResult<UniClient>();

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

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

        var addResult = await ExecuteAddUniClientRequest(addRequest);

        if (addResult.Success && request.SyncDns && !await DnsService.AddDnsARecord(addResult.Data.Hostname, addResult.Data.FixedIp, null))
        {
            Logger.LogError("Unable to create DNS Record for {HostName}:{Ip}", addResult.Data.Hostname, addResult.Data.FixedIp);
        }

        return addResult;
    }

    public async Task<ServiceResult> DeleteClient(string mac)
    {
        var result = new ServiceResult();

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

        var clientResult = await GetClient(mac);
        if (clientResult.Success)
        {
            if (clientResult.Data.Noted
                && (clientResult.Data.Notes.SyncDnsHostName ?? false)
                && !await DnsService.DeleteDnsARecord(clientResult.Data.Hostname, clientResult.Data.FixedIp, null))
            {
                Logger.LogError("Unable to remove DNS Record for {HostName}:{Ip}", clientResult.Data.Hostname,
                    clientResult.Data.FixedIp);
            }

            if (clientResult.Data.Noted && clientResult.Data.UseFixedIp)
            {
                await IpService.ReturnIpAddress(clientResult.Data.FixedIp);
            }
        }

        var postRequest = new UnifiRequests.StaRequest()
        {
            Cmd = "forget-sta",
            Macs = [mac]
        };

        var postRequestString = JsonConvert.SerializeObject(postRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

        var csrfToken = GetCsrfToken();

        if (!string.IsNullOrEmpty(csrfToken))
        {
            Logger.LogDebug("CSRF = {Csrf}", csrfToken);
            Logger.LogDebug("Payload String = {Payload}", postRequestString);
            try
            {
                var noteResult = await BaseSiteApiUrl
                    .AppendPathSegments(SiteId, "cmd", "stamgr")
                    .WithCookies(_cookieJar)
                    .WithHeader("X-Csrf-Token", csrfToken)
                    .PostStringAsync(postRequestString)
                    .ReceiveJson<UniResponse<List<UniClient>>>();

                if (noteResult.Meta.Rc == UniMeta.ErrorResponse)
                {
                    Logger.LogError("Error deleting editClientRequest : {Message}", noteResult.Meta.Msg);
                    result.MarkFailed($"Error deleting editClientRequest : {noteResult.Meta.Msg}");
                }
                else
                {
                    result.MarkSuccessful();
                }
            }
            catch (Exception e)
            {
                result.MarkFailed(e);
            }
        }
        else
        {
            Logger.LogDebug("CSRF Token is null");
            result.MarkFailed("No CSRF Token Present");
        }

        return result;
    }

    #endregion IUnifiService Implementation


    #region Private Methods

    private async Task<IEnumerable<UniClient>> GetAllFixedIpClients()
    {
        var data = await BaseSiteApiUrl.AppendPathSegments(SiteId, "stat", "alluser")
            .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

        if (data.Meta.Rc == UniMeta.ErrorResponse)
        {
            Logger.LogError("Error retrieving clients from {Url}: {Message}", UnifiOptions.Url, data.Meta.Msg);
            return [];
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
            return data.Data.Where(uc => uc.UseFixedIp);
        }
    }

    private async Task<ServiceResult<UniClient>> ExecuteAddUniClientRequest(UnifiRequests.AddUniClientRequest addRequest)
    {
        return await ExecuteRequest(BaseSiteApiUrl.AppendPathSegments(SiteId, "rest", "user"),
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
        var result = new ServiceResult<bool>();

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

        try
        {
            var data = await BaseSiteApiUrl.AppendPathSegments(SiteId, "rest", "user").SetQueryParam("mac", mac)
                .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

            if (data.Meta.Rc == UniMeta.ErrorResponse)
            {
                Logger.LogError("Error retrieving client from {Url}: {Message}", UnifiOptions.Url, data.Meta.Msg);
            }
            else
            {
                result.MarkSuccessful(data.Data.Count != 0);
            }
        }
        catch (Exception e)
        {
            result.MarkFailed(e);
        }

        return result;
    }
    private async Task<ServiceResult<UniClient>> GetClient(string mac)
    {
        var result = new ServiceResult<UniClient>();

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

        try
        {
            var data = await BaseSiteApiUrl.AppendPathSegments(SiteId, "rest", "user").SetQueryParam("mac", mac)
                .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

            if (data.Meta.Rc == UniMeta.ErrorResponse)
            {
                Logger.LogError("Error retrieving client from {Url}: {Message}", UnifiOptions.Url, data.Meta.Msg);
            }
            else
            {
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
            }
        }
        catch (Exception e)
        {
            result.MarkFailed(e);
        }

        return result;
    }

    private async Task<ServiceResult<List<UniClient>>> GetDevicesAsUniClient()
    {
        var result = new ServiceResult<List<UniClient>>();

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

        try
        {
            var devicesClients = new List<UniClient>();
            var data = await BaseSiteApiUrl.AppendPathSegments(SiteId, "stat", "device")
                .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniDevice>>>();

            if (data.Meta.Rc == UniMeta.ErrorResponse)
            {
                string message = data.Meta.Msg;
                Logger.LogError("Error retrieving clients from {Url}: {Message}", UnifiOptions.Url, message);
                result.MarkFailed($"Error retrieving clients from {UnifiOptions.Url}: {data.Meta.Msg}");
            }
            else
            {
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
