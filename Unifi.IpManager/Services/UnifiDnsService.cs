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
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;
using UnifiRequests = Unifi.IpManager.Models.Unifi.Requests;
using Unifi.IpManager.Options;
using System.IdentityModel.Tokens.Jwt;
using Unifi.IpManager.ExternalServices;


namespace Unifi.IpManager.Services;

public class UnifiDnsService(
    IOptions<UnifiControllerOptions> options,
    ILogger<UnifiDnsService> logger)
    : UnifiBaseService(options, logger), IUnifiDnsService
{

    #region IUnifiService Implementation


    public async Task<ServiceResult<List<HostDnsRecord>>> GetHostDnsRecords()
    {
        var result = new ServiceResult<List<HostDnsRecord>>();

        var devices = await GetDeviceDnsRecords();
        var dnsRecords = await GetAllStaticDns();

        var allRecords = devices.
                    Select(d => d.ToHostDnsRecord())
                    .Union(dnsRecords.Select(d => d.ToHostDnsRecord()))
                    .ToList();

        result.MarkSuccessful(allRecords);

        return result;
    }

    public async Task<ServiceResult<HostDnsRecord>> CreateHostDnsRecord(HostDnsRecord hostRecord)
    {
        var result = await ExecuteRequest(BaseDnsSiteUrl.AppendPathSegments(SiteId, "static-dns"),
        async (request) =>
            {
                var response = await request
                    .PostJsonAsync(new {
                        record_type = hostRecord.RecordType,
                        value = hostRecord.IpAddress,
                        key = hostRecord.Hostname,
                        enabled = true
                    })
                    .ReceiveJson<UniHostRecord>();
                return response.ToHostDnsRecord();
            }
        , true);
        return result;
    }

    public async Task<ServiceResult<HostDnsRecord>> UpdateDnsHostRecord(HostDnsRecord hostRecord)
    {
        var result = await ExecuteRequest(BaseDnsSiteUrl.AppendPathSegments(SiteId, "static-dns", hostRecord.Id),
        async (request) =>
            {

                var response = await request
                    .PutJsonAsync(hostRecord.ToUniHostRecord())
                    .ReceiveJson<UniHostRecord>();
                return response.ToHostDnsRecord();
            }
        , true);
        return result;
    }

    public async Task<ServiceResult> DeleteHostDnsRecord(string id)
    {
        // DELETE https://unifi.gerega.net/proxy/network/v2/api/site/default/static-dns/68a098ace250787265875126
        var result = await ExecuteRequest(BaseDnsSiteUrl.AppendPathSegments(SiteId, "static-dns", id),
        async (request) =>
            {
                await request.DeleteAsync();
                return string.Empty;
            }
        , true);
        return result;
    }

    #endregion IUnifiService Implementation


    #region Private Methods

    private async Task<List<UniHostRecord>> GetAllStaticDns()
    {
        try
        {
            var result = await ExecuteRequest(BaseDnsSiteUrl.AppendPathSegments(SiteId, "static-dns"),
                async (request) =>
                {
                    return await request.GetJsonAsync<List<UniHostRecord>>();
                });

            return result.Success ? result.Data : new List<UniHostRecord>();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error retrieving DNS records from {Url}: {Message}", UnifiOptions.Url, e.Message);
            return new List<UniHostRecord>();
        }
    }

    private async Task<List<UniDeviceDnsRecord>> GetDeviceDnsRecords()
    {
        try
        {
            var result =  await ExecuteRequest(BaseDnsSiteUrl.AppendPathSegments(SiteId, "static-dns", "devices"),
                async (request) =>
                {
                    return await request.GetJsonAsync<List<UniDeviceDnsRecord>>();
                });
            return result.Success ? result.Data : new List<UniDeviceDnsRecord>();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error retrieving DNS device records from {Url}: {Message}", UnifiOptions.Url, e.Message);
            return new List<UniDeviceDnsRecord>();
        }
    }




    #endregion Private Methods


}
