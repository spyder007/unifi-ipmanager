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

            var data = await UnifiOptions.Url.AppendPathSegments("api", "s", "at7as3rk", "stat", "alluser")
                .WithCookies(_cookieJar).GetJsonAsync<UniResponse<List<UniClient>>>();

            if (data.Meta.Rc == UniMeta.ErrorResponse)
            {
                Logger.LogError($"Error retrieving clients from {UnifiOptions.Url}: {data.Meta.Msg}");
            }

            return data.Data.Where(uc => uc.Use_fixedip).ToList();
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
                var randomGen = new Random();
                // TODO: Should get device list too
                var ipGroup = UnifiOptions.IpGroups.FirstOrDefault(g => g.Name == group);
                if (ipGroup != null)
                {
                    int tries = 0;
                    while (addRequest.fixed_ip == null || tries > 100)
                    {
                        ++tries;
                        foreach (var block in ipGroup.Blocks)
                        {
                            var assignedIp = $"192.168.1.{randomGen.Next(block.Min, block.Max)}";
                            if (clients.All(c => c.Fixed_ip != assignedIp))
                            {
                                addRequest.fixed_ip = assignedIp;
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
                .AppendPathSegments("api", "s", "at7as3rk", "rest", "user")
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

                var response =  await UnifiOptions.Url.AppendPathSegments("api","login").WithCookies(out _cookieJar).PostJsonAsync(credentials).ReceiveJson<UniResponse<List<string>>>();
                if (response.Meta.Rc == UniMeta.ErrorResponse)
                {
                    Logger.LogError($"Error logging on to ${UnifiOptions.Url}: {response.Meta.Msg}");
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
            for (int i = 0; i < 6; i++)
            {
                var number = r.Next(0, 255);
                b = Convert.ToByte(number);
                if (i == 0)
                {
                    b = SetBit(b, 6); //--> set locally administered
                    b = UnsetBit(b, 7); // --> set unicast 
                }
                sBuilder.Append(number.ToString("X2"));
                if (i < 5)
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
