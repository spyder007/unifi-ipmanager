using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using unifi.ipmanager.ExternalServices;
using unifi.ipmanager.Options;
using System.Linq;

namespace unifi.ipmanager.Services
{
    public class DnsService : IDnsService
    {
        private readonly ILogger<DnsService> _logger;
        private readonly DnsServiceOptions _options;

        public DnsService(ILogger<DnsService> logger, IOptions<DnsServiceOptions> dnsOptions)
        {
            _logger = logger;
            _options = dnsOptions.Value;
        }

        public async Task<bool> AddDnsARecord(string hostname, string ip, string zone)
        {
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
                _logger.LogError(ex, "Error Retrieving Dns Records for Hostname {hostname} | {zone}", hostname, zone);
                return null;
            }
        }
    }
}
