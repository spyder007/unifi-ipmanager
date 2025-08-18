using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using Unifi.IpManager.ExternalServices;
using Unifi.IpManager.Options;
using System.Linq;
using Spydersoft.Platform.Attributes;

namespace Unifi.IpManager.Services;

public class DnsService(ILogger<DnsService> logger, IOptions<DnsServiceOptions> dnsOptions) : IDnsService
{
    private readonly ILogger<DnsService> _logger = logger;
    private readonly DnsServiceOptions _options = dnsOptions.Value;

    public async Task<bool> AddDnsARecord(string hostname, string ip, string zone)
    {
        if (string.IsNullOrEmpty(_options.Url))
        {
            _logger.LogInformation("DNS Service not configured.  Ignoring AddDnsARecord({HostName}, {Ip}, {Zone})", hostname, ip, zone);
            return true;
        }

        using var httpClient = new HttpClient();
        var client = new Client(httpClient)
        {
            BaseUrl = _options.Url
        };

        var dnsRecord = new DnsRecord
        {
            HostName = hostname,
            ZoneName = zone ?? _options.DefaultZone,
            RecordType = DnsRecordType.A,
            Data = ip
        };

        var newRecord = await client.CreateRecordAsync(dnsRecord);

        return newRecord != null;
    }

    public Task<bool> AddDnsCNameRecord(string hostname, string alias, string zone)
    {
        throw new System.NotImplementedException();
    }

    public async Task<bool> BulkCreateDnsRecords(IEnumerable<DnsRecord> dnsRecords)
    {
        if (string.IsNullOrEmpty(_options.Url))
        {
            _logger.LogInformation("DNS Service not configured.  Ignoring BulkCreateDnsRecord - {Count} records", dnsRecords.Count());
            return true;
        }
        using var httpClient = new HttpClient();
        var client = new Client(httpClient)
        {
            BaseUrl = _options.Url
        };
        var results = await client.CreateDnsRecordsAsync(new BulkRecordRequest
        {
            Records = dnsRecords.ToList()
        });

        return results is { Count: > 0 };

    }

    public async Task<bool> DeleteDnsARecord(string hostname, string ip, string zone)
    {
        var recordToDelete = new DnsRecord
        {
            HostName = hostname,
            Data = ip,
            ZoneName = string.IsNullOrWhiteSpace(zone) ? _options.DefaultZone : zone,
            RecordType = DnsRecordType.A
        };

        return await DeleteDnsRecord(recordToDelete);
    }

    public async Task<bool> DeleteDnsRecord(DnsRecord dnsRecord)
    {
        if (string.IsNullOrEmpty(_options.Url))
        {
            _logger.LogInformation("DNS Service not configured.  Ignoring DeleteDnsRecord({Hostname}, {Ip}, {Zone})", dnsRecord.HostName, dnsRecord.Data, dnsRecord.ZoneName);
            return true;
        }

        try
        {
            using var httpClient = new HttpClient();
            var client = new Client(httpClient)
            {
                BaseUrl = _options.Url
            };
            await client.DeleteRecordAsync(dnsRecord);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Error Deleting DNS Record");
            return false;
        }

        return true;
    }

    public async Task<IEnumerable<DnsRecord>> GetDnsRecordsForHostname(string hostname, string zone)
    {
        if (string.IsNullOrEmpty(_options.Url))
        {
            _logger.LogInformation("DNS Service not configured.  Ignoring GetDnsRecordsForHostname({Hostname}, {Zone})", hostname, zone);
            return [];
        }

        try
        {
            using var httpClient = new HttpClient();
            var client = new Client(httpClient)
            {
                BaseUrl = _options.Url
            };
            var records = await client.GetRecordByHostnameAsync(hostname, zone ?? string.Empty);
            return records;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Error Retrieving Dns Records for Hostname {Hostname} | {Zone}", hostname, zone);
            return null;
        }
    }
}
