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
using unifi.ipmanager.Models;
using unifi.ipmanager.Models.DTO;
using unifi.ipmanager.Models.Unifi;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;
using UnifiRequests = unifi.ipmanager.Models.Unifi.Requests;

namespace unifi.ipmanager.Services
{
    public class UnifiService : IUnifiService
    {
        private const string SiteId = "at7as3rk";
        private const string NetworkId = "59f62826e4b0c5498bc2a82e";

        private IIpService IpService { get; }
        private UnifiControllerOptions UnifiOptions { get; }
        private ILogger Logger { get; }
        private CookieJar _cookieJar;

        public UnifiService(IOptions<UnifiControllerOptions> options, IIpService ipService, ILogger<UnifiService> logger)
        {
            IpService = ipService;
            UnifiOptions = options.Value;
            Logger = logger;
        }

        #region IUnifiService Implementation

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
                    if (devices.Data != null && devices.Data.Any())
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

        private async Task<IEnumerable<UniClient>> GetAllFixedIpClients()
        {
            var data = await UnifiOptions.Url.AppendPathSegments("api", "s", SiteId, "stat", "alluser")
                .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

            if (data.Meta.Rc == UniMeta.ErrorResponse)
            {
                Logger.LogError("Error retrieving clients from {url}: {message}", UnifiOptions.Url, data.Meta.Msg);
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
                return data.Data.Where(uc => uc.UseFixedIp);
            }
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

            var addRequest = new UnifiRequests.AddUniClientRequest
            {
                Mac = editClientRequest.MacAddress,
                Name = editClientRequest.Name,
                HostName = editClientRequest.Hostname,
                UseFixedIp = true,
                FixedIp = editClientRequest.IpAddress,
                NetworkId = NetworkId,
                Note = noteString
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

            var csrfToken = _cookieJar.FirstOrDefault(cookie => cookie.Name == "csrf_token");

            if (csrfToken != null)
            {
                Logger.LogDebug("CSRF = {csrf}", csrfToken.Value);
                Logger.LogDebug("Payload String = {payload}", postRequestString);
                try
                {
                    var noteResult = await UnifiOptions.Url
                        .AppendPathSegments("api", "s", SiteId, "rest", "user", clientResult.Data.Id)
                        .WithCookies(_cookieJar)
                        .WithHeader("X-Csrf-Token", csrfToken.Value)
                        .PutStringAsync(postRequestString)
                        .ReceiveJson<UniResponse<List<UniClient>>>();

                    if (noteResult.Meta.Rc == UniMeta.ErrorResponse)
                    {
                        Logger.LogError("Error updating client to {url}: {message}", UnifiOptions.Url, noteResult.Meta.Msg);
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

        public async Task<ServiceResult<UniClient>> ProvisionNewClient(string group, string name, string hostName, bool staticIp, bool syncDns)
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

            var macAddress = GenerateMacAddress();

            while (clients.Any(c => c.Mac == macAddress))
            {
                macAddress = GenerateMacAddress();
            }

            var note = new UniNote()
            {
                DnsHostname = hostName,
                SetOnDevice = false,
                SyncDnsHostName = syncDns
            };

            var noteString = JsonConvert.SerializeObject(note, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var addRequest = new UnifiRequests.AddUniClientRequest
            {
                Mac = macAddress,
                Name = name,
                HostName = hostName,
                UseFixedIp = staticIp,
                NetworkId = NetworkId,
                Note = noteString
            };

            if (staticIp)
            {
                var assignedIp = IpService.GetUnusedGroupIpAddress(group, clients.Select(c => c.FixedIp).ToList());
                if (!string.IsNullOrWhiteSpace(assignedIp))
                {
                    addRequest.FixedIp = assignedIp;
                }
            }

            return await ExecuteAddUniClientRequest(addRequest);
        }

        public async Task<ServiceResult> DeleteClient(string mac)
        {
            var result = new ServiceResult();

            if (!await VerifyLogin())
            {
                result.MarkFailed("Login failed.");
                return result;
            }

            var postRequest = new UnifiRequests.StaRequest()
            {
                Cmd = "forget-sta",
                Macs = new List<string> { mac }
            };

            var postRequestString = JsonConvert.SerializeObject(postRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            var csrfToken = _cookieJar.FirstOrDefault(cookie => cookie.Name == "csrf_token");

            if (csrfToken != null)
            {
                Logger.LogDebug("CSRF = {csrf}", csrfToken.Value);
                Logger.LogDebug("Payload String = {payload}", postRequestString);
                try
                {
                    var noteResult = await UnifiOptions.Url
                        .AppendPathSegments("api", "s", SiteId, "cmd", "stamgr")
                        .WithCookies(_cookieJar)
                        .WithHeader("X-Csrf-Token", csrfToken.Value)
                        .PostStringAsync(postRequestString)
                        .ReceiveJson<UniResponse<List<UniClient>>>();

                    if (noteResult.Meta.Rc == UniMeta.ErrorResponse)
                    {
                        Logger.LogError("Error deleting editClientRequest : {message}", noteResult.Meta.Msg);
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

        private async Task<ServiceResult<UniClient>> ExecuteAddUniClientRequest(UnifiRequests.AddUniClientRequest addRequest)
        {
            var result = new ServiceResult<UniClient>();

            var addRequestString = JsonConvert.SerializeObject(addRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            var csrfToken = _cookieJar.FirstOrDefault(cookie => cookie.Name == "csrf_token");

            if (csrfToken != null)
            {
                Logger.LogDebug("CSRF = {csrf}", csrfToken.Value);
                Logger.LogDebug("Payload String = {payload}", addRequestString);
                try
                {
                    var addResult = await UnifiOptions.Url
                        .AppendPathSegments("api", "s", SiteId, "rest", "user")
                        .WithCookies(_cookieJar)
                        .WithHeader("X-Csrf-Token", csrfToken.Value)
                        .PostStringAsync(addRequestString)
                        .ReceiveJson<UniResponse<List<UniClient>>>();

                    if (addResult.Meta.Rc == UniMeta.ErrorResponse)
                    {
                        Logger.LogError("Error adding client to {url}: {message}", UnifiOptions.Url, addResult.Meta.Msg);
                        result.MarkFailed($"Error adding client to {UnifiOptions.Url}: {addResult.Meta.Msg}");
                        return result;
                    }

                    result.MarkSuccessful(addResult.Data[0]);
                }
                catch (Exception e)
                {
                    result.MarkFailed(e);
                    return result;
                }
            }
            else
            {
                Logger.LogDebug("CSRF Token is null");
                result.MarkFailed("No CSRF Token Present");
            }

            return result;
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
                var data = await UnifiOptions.Url.AppendPathSegments("api", "s", SiteId, "rest", "user").SetQueryParam("mac", mac)
                    .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

                if (data.Meta.Rc == UniMeta.ErrorResponse)
                {
                    Logger.LogError("Error retrieving client from {url}: {message}", UnifiOptions.Url, data.Meta.Msg);
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
                var data = await UnifiOptions.Url.AppendPathSegments("api", "s", SiteId, "rest", "user").SetQueryParam("mac", mac)
                    .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

                if (data.Meta.Rc == UniMeta.ErrorResponse)
                {
                    Logger.LogError("Error retrieving client from {url}: {message}", UnifiOptions.Url, data.Meta.Msg);
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

                var data = await UnifiOptions.Url.AppendPathSegments("api", "s", SiteId, "stat", "device")
                    .WithCookies(_cookieJar).GetJsonAsync();

                if (data.meta.rc == UniMeta.ErrorResponse)
                {
                    string message = data.Meta.Msg;
                    Logger.LogError("Error retrieving clients from {url}: {message}", UnifiOptions.Url, message);
                    result.MarkFailed($"Error retrieving clients from {UnifiOptions.Url}: {data.Meta.Msg}");
                }
                else
                {
                    foreach (var client in data.data)
                    {
                        devicesClients.Add(new UniClient
                        {
                            Id = client._id,
                            FixedIp = client.config_network.ip,
                            Name = client.name,
                            Mac = client.mac,
                            UseFixedIp = true,
                            ObjectType = "device",
                            IpGroup = IpService.GetIpGroupForAddress(client.config_network.ip)
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

        private async Task<bool> VerifyLogin()
        {
            if (_cookieJar == null || _cookieJar.Count == 0)
            {
                var credentials = new
                {
                    username = UnifiOptions.Username,
                    password = UnifiOptions.Password,
                    remember = false,
                    strict = true
                };

                try
                {
                    _ = await UnifiOptions.Url.AppendPathSegments("api", "login").WithCookies(out _cookieJar).PostJsonAsync(credentials).ReceiveJson<UniResponse<List<string>>>();
                }
                catch (FlurlHttpException ex)
                {
                    var errorResponse = await ex.GetResponseJsonAsync<UniResponse<List<string>>>();
                    if (errorResponse.Meta.Rc == UniMeta.ErrorResponse)
                    {
                        Logger.LogError("Error logging on to {url}: {message}", UnifiOptions.Url, errorResponse.Meta.Msg);
                        return false;
                    }
                    Logger.LogDebug("Error Logging in: URL - {url}, UserName - {username}, {password}", UnifiOptions.Url, UnifiOptions.Username, UnifiOptions.Password);
                    Logger.LogError(ex, "Error logging in to Unifi Controller");
                    return false;
                }
            }
            return true;
        }

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
    }
}