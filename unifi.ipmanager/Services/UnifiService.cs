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
                var data = await UnifiOptions.Url.AppendPathSegments("api", "s", SiteId, "stat", "alluser")
                    .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

                if (data.Meta.Rc == UniMeta.ErrorResponse)
                {
                    Logger.LogError($"Error retrieving clients from {UnifiOptions.Url}: {data.Meta.Msg}");
                }
                else
                {
                    data.Data.ForEach(client =>
                    {
                        if (string.IsNullOrWhiteSpace(client.Name))
                        {
                            client.Name = client.Hostname;
                        }
                        if (client.Use_fixedip)
                        {
                            client.IpGroup = IpService.GetIpGroupForAddress(client.Fixed_ip);
                        }
                    });

                    allClients.AddRange(data.Data.Where(uc => uc.Use_fixedip));
                }
            }
            catch (Exception e)
            {
                result.MarkFailed(e);
                return result;
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
                name = editClientRequest.Name,
                hostname = editClientRequest.Hostname
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

            editRequest.note = JsonConvert.SerializeObject(updateNotes, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            var postRequestString = JsonConvert.SerializeObject(editRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            var csrfToken = _cookieJar.FirstOrDefault(cookie => cookie.Name == "csrf_token");

            Logger.LogDebug($"CSRF = {csrfToken.Value}");
            Logger.LogDebug($"Payload String = {postRequestString}");
            try
            {
                var noteResult = await UnifiOptions.Url
                    .AppendPathSegments("api", "s", SiteId, "rest", "user", clientResult.Data._id)
                    .WithCookies(_cookieJar)
                    .WithHeader("X-Csrf-Token", csrfToken.Value)
                    .PutStringAsync(postRequestString)
                    .ReceiveJson<UniResponse<List<UniClient>>>();

                if (noteResult.Meta.Rc == UniMeta.ErrorResponse)
                {
                    var error = $"Error updating client to {UnifiOptions.Url}: {noteResult.Meta.Msg}";
                    Logger.LogError(error);
                    result.MarkFailed(error);
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
                Dns_hostname = hostName,
                Set_on_device = false,
                Sync_dnshostname = syncDns
            };

            var noteString = JsonConvert.SerializeObject(note, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var addRequest = new UnifiRequests.AddUniClientRequest
            {
                mac = macAddress,
                name = name,
                hostname = hostName,
                use_fixedip = staticIp,
                network_id = NetworkId,
                note = noteString
            };

            if (staticIp)
            {
                var assignedIp = IpService.GetUnusedGroupIpAddress(group, clients.Select(c => c.Fixed_ip).ToList());
                if (!string.IsNullOrWhiteSpace(assignedIp))
                {
                    addRequest.fixed_ip = assignedIp;
                }
            }

            var addRequestString = JsonConvert.SerializeObject(addRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            var csrfToken = _cookieJar.FirstOrDefault(cookie => cookie.Name == "csrf_token");

            Logger.LogDebug($"CSRF = {csrfToken.Value}");
            Logger.LogDebug($"Payload String = {addRequestString}");
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
                    var error = $"Error adding client to {UnifiOptions.Url}: {addResult.Meta.Msg}";
                    Logger.LogError(error);
                    result.MarkFailed(error);
                    return result;
                }
                result.MarkSuccessful(addResult.Data[0]);
            }
            catch (Exception e)
            {
                result.MarkFailed(e);
                return result;
            }

            return result;
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
                cmd = "forget-sta",
                macs = new List<string> { mac }
            };

            var postRequestString = JsonConvert.SerializeObject(postRequest, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            var csrfToken = _cookieJar.FirstOrDefault(cookie => cookie.Name == "csrf_token");

            Logger.LogDebug($"CSRF = {csrfToken.Value}");
            Logger.LogDebug($"Payload String = {postRequestString}");
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
                    var error = $"Error deleting editClientRequest : {noteResult.Meta.Msg}";
                    Logger.LogError(error);
                    result.MarkFailed(error);
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

            return result;
        }

        #endregion IUnifiService Implementation

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
                    Logger.LogError($"Error retrieving client from {UnifiOptions.Url}: {data.Meta.Msg}");
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
                            if (client.Use_fixedip)
                            {
                                client.IpGroup = IpService.GetIpGroupForAddress(client.Fixed_ip);
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
                    var error = $"Error retrieving clients from {UnifiOptions.Url}: {data.Meta.Msg}";
                    Logger.LogError(error);
                    result.MarkFailed(error);
                }
                else
                {
                    foreach (var client in data.data)
                    {
                        devicesClients.Add(new UniClient
                        {
                            _id = client._id,
                            Fixed_ip = client.config_network.ip,
                            Name = client.name,
                            Mac = client.mac,
                            Use_fixedip = true,
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
                    var response = await UnifiOptions.Url.AppendPathSegments("api", "login").WithCookies(out _cookieJar).PostJsonAsync(credentials).ReceiveJson<UniResponse<List<string>>>();
                }
                catch (FlurlHttpException ex)
                {
                    var errorResponse = await ex.GetResponseJsonAsync<UniResponse<List<string>>>();
                    if (errorResponse.Meta.Rc == UniMeta.ErrorResponse)
                    {
                        Logger.LogError($"Error logging on to ${UnifiOptions.Url}: {errorResponse.Meta.Msg}");
                        return false;
                    }
                    Logger.LogDebug($"Error Logging in:0 URL - {UnifiOptions.Url}, UserName - {UnifiOptions.Username}, {UnifiOptions.Password}");
                    Logger.LogError(ex, "Error logging in to Unifi Controller");
                    return false;
                }
            }
            return true;
        }

        private string GenerateMacAddress()
        {
            var sBuilder = new StringBuilder();
            var r = new Random();
            byte b;
            sBuilder.Append("00:15:5D:");
            for (int i = 0; i < 3; i++)
            {
                var number = r.Next(0, 255);
                b = Convert.ToByte(number);
                //if (i == 0)
                //{
                //    b = SetBit(b, 6); //--> set locally administered
                //    b = UnsetBit(b, 7); // --> set unicast
                //}
                sBuilder.Append(number.ToString("X2"));
                if (i < 2)
                {
                    sBuilder.Append(":");
                }
            }
            return sBuilder.ToString().ToUpper();
        }

        private byte SetBit(byte b, int bitNumber)
        {
            if (bitNumber < 8 && bitNumber > -1)
            {
                return (byte)(b | (byte)(0x01 << bitNumber));
            }
            else
            {
                Logger.LogError(new InvalidOperationException(
                    $"The value for {bitNumber} was not in the proper range (BitNumber = (min)0 - (max)7)"), "Error setting bit");
            }
            return default;
        }

        private byte UnsetBit(byte b, int bitNumber)
        {
            if (bitNumber < 8 && bitNumber > -1)
            {
                return (byte)(b | (byte)(0x00 << bitNumber));
            }
            else
            {
                Logger.LogError(new InvalidOperationException(
                    $"The value for {bitNumber} was not in the proper range (BitNumber = (min)0 - (max)7)"), "Error unsetting bit");
            }

            return default;
        }
    }
}