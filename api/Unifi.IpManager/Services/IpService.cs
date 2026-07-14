using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Unifi.IpManager.Options;
using System.Threading.Tasks;
using Spydersoft.Platform.Attributes;
using Unifi.IpManager.Models.Unifi;
using System.Net;

namespace Unifi.IpManager.Services;

[DependencyInjection(typeof(IIpService), LifetimeOfService.Scoped)]
public class IpService(IOptions<IpOptions> options, ILogger<IpService> logger, IDistributedCache distributedCache) : IIpService
{
    private IpOptions IpOptions { get; } = options.Value;
    private ILogger Logger { get; } = logger;

    private IDistributedCache Cache { get; } = distributedCache;

    private const string IpCooldownCacheKeyTemplate = "Unifi.IpManager.IpCooldown.{0}";

    public async Task<string> GetUnusedNetworkIpAddress(UnifiNetwork network, List<string> usedIps)
    {
        ArgumentNullException.ThrowIfNull(usedIps);
        ArgumentNullException.ThrowIfNull(network);

        if (string.IsNullOrWhiteSpace(network.IpSubnet) || string.IsNullOrWhiteSpace(network.DhcpStartAddress))
        {
            Logger.LogWarning("Network {NetworkName} is missing IpSubnet or DhcpStartAddress", network.Name);
            return string.Empty;
        }

        // Parse the subnet to get the base IP and mask (e.g., "192.168.10.0/23")
        var subnetParts = network.IpSubnet.Split('/');
        if (subnetParts.Length != 2)
        {
            Logger.LogWarning("Invalid subnet format for network {NetworkName}: {Subnet}", network.Name, network.IpSubnet);
            return string.Empty;
        }

        var baseIpAddress = subnetParts[0];
        if (!IPAddress.TryParse(baseIpAddress, out var parsedBaseIp))
        {
            Logger.LogWarning("Invalid base IP address for network {NetworkName}: {BaseIp}", network.Name, baseIpAddress);
            return string.Empty;
        }

        if (!int.TryParse(subnetParts[1], out var prefixLength) || prefixLength < 0 || prefixLength > 32)
        {
            Logger.LogWarning("Invalid subnet prefix length for network {NetworkName}: {Prefix}", network.Name, subnetParts[1]);
            return string.Empty;
        }

        // Parse DHCP start address to determine the upper limit
        if (!IPAddress.TryParse(network.DhcpStartAddress, out var dhcpStartIp))
        {
            Logger.LogWarning("Invalid DHCP start address for network {NetworkName}: {DhcpStart}", network.Name, network.DhcpStartAddress);
            return string.Empty;
        }

        // Calculate the network address and starting IP
        var baseOctets = parsedBaseIp.GetAddressBytes();
        var dhcpStartOctets = dhcpStartIp.GetAddressBytes();

        // Calculate subnet mask from prefix length
        var subnetMask = GetSubnetMask(prefixLength);
        var subnetMaskBytes = subnetMask.GetAddressBytes();

        // Calculate network address by ANDing base IP with subnet mask
        var networkAddress = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            networkAddress[i] = (byte) (baseOctets[i] & subnetMaskBytes[i]);
        }

        // Start at network address + 10 (converted to uint for easier arithmetic)
        var startIpValue = IpToUInt(networkAddress) + 10;
        var startIpBytes = UIntToIp(startIpValue);

        int tries = 0;
        while (tries < 100)
        {
            ++tries;

            // Iterate through IP addresses from start to the DHCP start address
            var currentIpBytes = (byte[]) startIpBytes.Clone();
            while (CompareIpAddresses(currentIpBytes, dhcpStartOctets) < 0)
            {
                var candidateIp = new IPAddress(currentIpBytes).ToString();

                if (usedIps.TrueForAll(ip => ip != candidateIp) && !await IpInCooldown(candidateIp))
                {
                    return candidateIp;
                }

                // Increment IP address
                if (!IncrementIpAddress(currentIpBytes))
                {
                    break;
                }
            }
        }

        Logger.LogWarning("No open IPs found for network {NetworkName}", network.Name);
        return string.Empty;
    }

    private static bool IncrementIpAddress(byte[] octets)
    {
        for (int i = octets.Length - 1; i >= 0; i--)
        {
            if (octets[i] < 255)
            {
                octets[i]++;
                return true;
            }
            octets[i] = 0;
        }
        return false; // Overflow
    }

    private static int CompareIpAddresses(byte[] ip1, byte[] ip2)
    {
        for (int i = 0; i < ip1.Length; i++)
        {
            if (ip1[i] < ip2[i])
            {
                return -1;
            }

            if (ip1[i] > ip2[i])
            {
                return 1;
            }
        }
        return 0;
    }

    private static IPAddress GetSubnetMask(int prefixLength)
    {
        // Create a 32-bit mask with prefixLength bits set to 1
        uint mask = prefixLength == 0 ? 0 : 0xFFFFFFFF << (32 - prefixLength);

        // Convert to bytes in network order (big-endian)
        byte[] bytes = new byte[4];
        bytes[0] = (byte) (mask >> 24);
        bytes[1] = (byte) (mask >> 16);
        bytes[2] = (byte) (mask >> 8);
        bytes[3] = (byte) mask;

        return new IPAddress(bytes);
    }

    private static uint IpToUInt(byte[] ipBytes)
    {
        return ((uint) ipBytes[0] << 24) | ((uint) ipBytes[1] << 16) | ((uint) ipBytes[2] << 8) | ipBytes[3];
    }

    private static byte[] UIntToIp(uint value)
    {
        return new byte[]
        {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
    }

    public async Task ReturnIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return;
        }
        string cacheKey = GetCooldownKey(ipAddress);
        // When the IP is returned, set a record in the cache with an absolute expiration
        try
        {
            var cachedIp = await Cache.GetAsync(cacheKey);
            if (cachedIp == null)
            {
                var dnsOptions =
                    new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTime.Now.AddMinutes(IpOptions.IpCooldownMinutes));
                await Cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(ipAddress), dnsOptions);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error caching returned IP address {IpAddress}", ipAddress);
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
