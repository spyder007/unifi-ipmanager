using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spydersoft.Platform.Attributes;
using Unifi.IpManager.Extensions;
using Unifi.IpManager.Models.Dns;
using Unifi.IpManager.Models.DTO;

namespace Unifi.IpManager.Services;

[DependencyInjection(typeof(IClusterDnsService), LifetimeOfService.Scoped)]
public class ClusterDnsService(
    IUnifiDnsService unifiDnsService,
    ILogger<ClusterDnsService> logger
) : IClusterDnsService
{

    private readonly IUnifiDnsService UnifiDnsService = unifiDnsService;
    private readonly ILogger<ClusterDnsService> Logger = logger;

    public async Task<ServiceResult<ClusterDns>> CreateClusterDns(NewClusterRequest clusterDns)
    {
        Logger.LogTrace("Processing request to create new ClusterDns Record");
        try
        {
            var controlPlaneHost = $"cp-{clusterDns.Name}";
            var trafficHost = $"tfx-{clusterDns.Name}";
            var success = true;
            var recordsToCreate = new List<HostDnsRecord>();
            foreach (var serverIp in clusterDns.ControlPlaneIps)
            {
                recordsToCreate.Add(new HostDnsRecord
                {
                    Hostname = $"{controlPlaneHost}.{clusterDns.ZoneName}",
                    IpAddress = serverIp,
                    RecordType = "A"
                });
            }

            foreach (var trafficIp in clusterDns.TrafficIps)
            {
                recordsToCreate.Add(new HostDnsRecord
                {
                    Hostname = $"{trafficHost}.{clusterDns.ZoneName}",
                    IpAddress = trafficIp,
                    RecordType = "A"
                });
            }

            foreach (var record in recordsToCreate)
            {
                var createResult = await UnifiDnsService.CreateHostDnsRecord(record);
                if (!createResult.Success)
                {
                    success = false;
                }
            }

            return !success
                ? new ServiceResult<ClusterDns>
                {
                    Success = false
                }
                : await GetClusterDns(clusterDns.Name, clusterDns.ZoneName);
        }
        catch (Exception ex)
        {
            return new ServiceResult<ClusterDns>
            {
                Success = false,
                Errors =
                [
                    ex.Message
                ]
            };
        }
    }

    public async Task<ServiceResult<ClusterDns>> GetClusterDns(string name, string zone)
    {
        Logger.LogTrace("Processing request for all ClusterDns Records");
        try
        {
            var dnsRecords = await UnifiDnsService.GetHostDnsRecords();

            if (!dnsRecords.Success)
            {
                return new ServiceResult<ClusterDns>
                {
                    Success = false,
                    Errors = dnsRecords.Errors
                };
            }

            // need at least one CP for a cluster definition
            if (!dnsRecords.Data.Any(r => r.Hostname.StartsWith($"cp-{name}")))
            {
                return new ServiceResult<ClusterDns>
                {
                    Success = false,
                    Errors = ["No control plane records found for the specified cluster."]
                };
            }

            var clusterDns = new ClusterDns
            {
                Name = name,
                ZoneName = zone,
                ControlPlane = dnsRecords.Data.Where(r => r.Hostname.StartsWith($"cp-{name}")).ToList(),
                Traffic = dnsRecords.Data.Where(r => r.Hostname.StartsWith($"tfx-{name}")).ToList()
            };

            if (string.IsNullOrEmpty(clusterDns.ZoneName))
            {
                // If no zone name is set, use the first record's zone name
                clusterDns.ZoneName = clusterDns.ControlPlane.FirstOrDefault()?.Hostname.GetDomainFromHostname();
            }

            return new ServiceResult<ClusterDns>()
            {
                Success = true,
                Data = clusterDns
            };
        }
        catch (Exception ex)
        {
            return new ServiceResult<ClusterDns>
            {
                Errors =
                [
                    ex.Message
                ],
                Success = false
            };
        }

    }

    public async Task<ServiceResult<ClusterDns>> UpdateClusterDns(ClusterDns clusterDns)
    {
        Logger.LogTrace("Processing request to update ClusterDns Record");
        try
        {
            var existingRecordResult = await GetClusterDns(clusterDns.Name, clusterDns.ZoneName);

            if (!existingRecordResult.Success || existingRecordResult.Data is not ClusterDns existingRecord)
            {
                return new ServiceResult<ClusterDns>
                {
                    Success = false,
                    Errors = [$"Cluster {clusterDns.Name} does not exist."]
                };
            }

            var cpProcessResult = await ProcessHostDnsRecords(existingRecord.ControlPlane, clusterDns.ControlPlane);
            var tfProcessResult = await ProcessHostDnsRecords(existingRecord.Traffic, clusterDns.Traffic);

            var success = cpProcessResult.Success && tfProcessResult.Success;        

            return !success
                ? new ServiceResult<ClusterDns>
                {
                    Success = false,
                    Errors = cpProcessResult.Errors.Concat(tfProcessResult.Errors).ToList()
                }
                : await GetClusterDns(clusterDns.Name, clusterDns.ZoneName);
        }
        catch (Exception ex)
        {
            return new ServiceResult<ClusterDns>
            {
                Success = false,
                Errors =
                [
                    ex.Message
                ]
            };
        }
    }


    private async Task<ServiceResult> ProcessHostDnsRecords(List<HostDnsRecord> existingRecords, List<HostDnsRecord> requestedRecords)
    {
        var processResult = new ServiceResult();
        processResult.MarkSuccessful();

        // process new records first.
        foreach (var newRecord in requestedRecords.Where(r => string.IsNullOrWhiteSpace(r.Id)))
        {
            var createResult = await UnifiDnsService.CreateHostDnsRecord(newRecord);
            if (!createResult.Success)
            {
                processResult.MarkFailed(createResult.Errors);
            }
        }

        foreach (var existingRecordId in existingRecords.Select(r => r.Id))
        {
           // Check if the record is in the requested records
            var requestedRecord = requestedRecords.FirstOrDefault(r => r.Id == existingRecordId);

            if (requestedRecord != null)
            {
                // Update Existing Records
                var updateResult = await UnifiDnsService.UpdateDnsHostRecord(requestedRecord);
                if (!updateResult.Success)
                {
                    processResult.MarkFailed(updateResult.Errors);
                }
            }
            else
            {
                // Existing record no longer on the requested collection, delete it.
                var deleteResult = await UnifiDnsService.DeleteHostDnsRecord(existingRecordId);
                if (!deleteResult.Success)
                {
                    processResult.MarkFailed(deleteResult.Errors);
                }
            }
        }
        return processResult;
    }
}
