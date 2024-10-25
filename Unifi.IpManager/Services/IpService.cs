using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Unifi.IpManager.Options;
using System.Threading.Tasks;

namespace Unifi.IpManager.Services
{
    public class IpService(IOptions<IpOptions> options, ILogger<UnifiService> logger, IDistributedCache distributedCache) : IIpService
    {
        private IpOptions IpOptions { get; } = options.Value;
        private ILogger Logger { get; } = logger;

        private IDistributedCache Cache { get; } = distributedCache;

        private const string IpCooldownCacheKeyTemplate = "Unifi.IpManager.IpCooldown.{0}";

        public async Task<string> GetUnusedGroupIpAddress(string name, List<string> usedIps)
        {
            var ipGroup = IpOptions.IpGroups.Find(g => g.Name == name);

            if (ipGroup == null)
            {
                return string.Empty;
            }

            int tries = 0;
            while (tries < 100)
            {
                ++tries;
                foreach (var block in ipGroup.Blocks)
                {
                    var lastIpDigit = block.Min;
                    while (lastIpDigit < block.Max)
                    {
                        var assignedIp = $"192.168.1.{lastIpDigit}";
                        if (usedIps.TrueForAll(ip => ip != assignedIp) && !await IpInCooldown(assignedIp))
                        {
                            return assignedIp;
                        }

                        ++lastIpDigit;
                    }
                }
            }
            Logger.LogWarning("No open IPs found for {GroupName}", ipGroup.Name);
            return string.Empty;
        }

        public string GetIpGroupForAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return string.Empty;
            }

            var match = System.Text.RegularExpressions.Regex.Match(ipAddress, "(\\d{1,3})\\.(\\d{1,3})\\.(\\d{1,3})\\.(\\d{1,3})");
            if (!match.Success)
            {
                return string.Empty;
            }

            var lastIp = int.Parse(match.Groups[4].Value);


            var group = IpOptions.IpGroups.Find(group => group.Blocks.Exists(b => b.Min <= lastIp && b.Max >= lastIp));
            return group != null ? group.Name : string.Empty;
        }

        public async Task ReturnIpAddress(string ipAddress)
        {
            string cacheKey = GetCooldownKey(ipAddress);
            // When the IP is returned, set a record in the cache with an absolute expiration
            var cachedIp = await Cache.GetAsync(cacheKey);
            if (cachedIp == null)
            {
                var dnsOptions =
                    new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddMinutes(IpOptions.IpCooldownMinutes));
                await Cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(ipAddress), dnsOptions);
            }
        }

        private async Task<bool> IpInCooldown(string ipAddress)
        {
            var cachedIp = await Cache.GetAsync(GetCooldownKey(ipAddress));
            return cachedIp != null;
        }

        private static string GetCooldownKey(string ipAddress)
        {
            return string.Format(IpCooldownCacheKeyTemplate, ipAddress);
        }

    }
}
