using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using unifi.ipmanager.Options;

namespace unifi.ipmanager.Services
{
    public class IpService : IIpService
    {
        private IpOptions IpOptions { get; }
        private ILogger Logger { get; }

        public IpService(IOptions<IpOptions> options, ILogger<UnifiService> logger)
        {
            IpOptions = options.Value;
            Logger = logger;
        }

        public string GetUnusedGroupIpAddress(string name, List<string> usedIps)
        {
            var ipGroup = IpOptions.IpGroups.FirstOrDefault(g => g.Name == name);

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
                        if (usedIps.All(ip => ip != assignedIp))
                        {
                            return assignedIp;
                        }

                        ++lastIpDigit;
                    }
                }
            }
            Logger.LogWarning($"No open IPs found for {ipGroup.Name}");
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


            var group = IpOptions.IpGroups.FirstOrDefault(group => group.Blocks.Any(b => b.Min <= lastIp && b.Max >= lastIp));
            return group != null ? group.Name : string.Empty;
        }
    }
}
