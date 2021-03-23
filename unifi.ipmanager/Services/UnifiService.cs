using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Flurl;
using Flurl.Http;
using Flurl.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using unifi.ipmanager.Models;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;

namespace unifi.ipmanager.Services
{
    public class UnifiService : IUnifiService
    {
        private const string SiteId = "at7as3rk";

        private UnifiControllerOptions UnifiOptions { get; }
        private ILogger Logger { get; }

        private FlurlClient _client;

        private CookieJar _cookieJar;

        public UnifiService(IOptions<UnifiControllerOptions> options, ILogger<UnifiService> logger)
        {
            UnifiOptions = options.Value;
            Logger = logger;
        }


        public async Task<List<UniClient>> GetAllFixedClients()
        {
            await VerifyLogin();

            var allClients = new List<UniClient>();

            var data = await UnifiOptions.Url.AppendPathSegments("api", "s", SiteId, "stat", "alluser")
                .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

            if (data.Meta.Rc == UniMeta.ErrorResponse)
            {
                Logger.LogError($"Error retrieving clients from {UnifiOptions.Url}: {data.Meta.Msg}");
            }
            else
            {
                allClients.AddRange(data.Data.Where(uc => uc.Use_fixedip));
            }

            var devices = await GetDevicesAsUniClient();
            if (devices != null && devices.Any())
            {
                allClients.AddRange(devices);
            }

            return allClients;
        }

        public async Task<UniClient> ProvisionNewClient(string group, string name, string hostName, bool staticIp)
        {
            await VerifyLogin();
            var clients = await GetAllFixedClients();
            var macAddress = GenerateMacAddress();

            while (clients.Any(c => c.Mac == macAddress))
            {
                macAddress = GenerateMacAddress();
            }

            var addRequest = new AddUniClientRequest
            {
                mac = macAddress,
                name = name,
                hostname = hostName,
                use_fixedip = staticIp,
                network_id = "59f62826e4b0c5498bc2a82e"
            };


            if (staticIp)
            {
                var ipGroup = UnifiOptions.IpGroups.FirstOrDefault(g => g.Name == group);
                if (ipGroup != null)
                {
                    int tries = 0;
                    bool ipAssigned = false;
                    while (!ipAssigned || tries < 100)
                    {
                        ++tries;
                        foreach (var block in ipGroup.Blocks)
                        {
                            var lastIpDigit = block.Min;
                            while (lastIpDigit < block.Max)
                            {
                                var assignedIp = $"192.168.1.{lastIpDigit}";
                                if (clients.All(c => c.Fixed_ip != assignedIp))
                                {
                                    addRequest.fixed_ip = assignedIp;
                                    ipAssigned = true;
                                    break;
                                }
                             
                                ++lastIpDigit;
                            }
                            if (ipAssigned)
                            {
                                break;
                            }
                        }
                    }
                }

            }

            var addRequestString = JsonConvert.SerializeObject(addRequest, new JsonSerializerSettings() {NullValueHandling = NullValueHandling.Ignore});

            var csrfToken = _cookieJar.FirstOrDefault(cookie => cookie.Name == "csrf_token");

            Logger.LogDebug($"CSRF = {csrfToken.Value}");
            Logger.LogDebug($"Payload String = {addRequestString}");

            var addResult = await UnifiOptions.Url
                .AppendPathSegments("api", "s", SiteId, "rest", "user")
                .WithCookies(_cookieJar)
                .WithHeader("X-Csrf-Token", csrfToken.Value)
                .PostStringAsync(addRequestString)
                .ReceiveJson<UniResponse<List<UniClient>>>();

            if (addResult.Meta.Rc == UniMeta.ErrorResponse)
            {
                Logger.LogError($"Error adding client to {UnifiOptions.Url}: {addResult.Meta.Msg}");
                return null;
            }

            return addResult.Data[0];
        }

        private async Task<List<UniClient>> GetDevicesAsUniClient()
        {
            await VerifyLogin();

            var devicesClients = new List<UniClient>();

            var data = await UnifiOptions.Url.AppendPathSegments("api", "s", SiteId, "stat", "device")
                .WithCookies(_cookieJar).GetJsonAsync();

            if (data.meta.rc == UniMeta.ErrorResponse)
            {
                Logger.LogError($"Error retrieving clients from {UnifiOptions.Url}: {data.Meta.Msg}");
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
                        Use_fixedip = true
                    });
                }
            }

            return devicesClients;
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

        #region Unifi Client Requests
        private class AddUniClientRequest
        {
            public string mac { get; set; }

            public string name { get; set; }

            public string hostname { get; set; }

            public bool use_fixedip { get; set; }

            public string network_id { get; set; }

            public string fixed_ip { get; set; }
        }

        #endregion

    }

}
