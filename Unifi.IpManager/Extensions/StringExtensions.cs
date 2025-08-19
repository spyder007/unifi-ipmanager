using System;
using System.Linq;

namespace Unifi.IpManager.Extensions;

public static class StringExtensions
{
    public static string GetDomainFromHostname(this string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException("Invalid hostname", nameof(hostname));
        }

        var parts = hostname.Split('.');
        return parts.Length > 2 ? string.Join(".", parts.Skip(1)) : hostname;
    }
}
