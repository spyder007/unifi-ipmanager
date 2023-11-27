using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using unifi.ipmanager.Models.Dns;
using unifi.ipmanager.Models.DTO;
using unifi.ipmanager.Services;
using unifi.ipmanager.ExternalServices;

namespace unifi.ipmanager.Controllers
{
    [ApiVersion("1.0")]
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class ClusterDnsController
    {
        private readonly ILogger<ClientController> _logger;

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        private IDnsService DnsService { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientController"/> class.
        /// </summary>
        /// <param name="dnsService">The unifi service.</param>
        /// <param name="logger">The logger.</param>
        public ClusterDnsController(IDnsService dnsService, ILogger<ClientController> logger)
        {
            DnsService = dnsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>ActionResult&lt;System.String&gt;.</returns>
        [HttpGet("{name}")]
        public async Task<ActionResult<ServiceResult<ClusterDns>>> Get([FromRoute] string name, [FromQuery] string zone)
        {
            _logger.LogTrace("Processing request for all ClusterDns Records");
            try
            {
                var clusterDns = new ClusterDns
                {
                    Name = name,
                    ZoneName = zone,
                    ControlPlane = (await DnsService.GetDnsRecordsForHostname($"cp-{name}", zone)).ToList(),
                    Traffic = (await DnsService.GetDnsRecordsForHostname($"tfx-{name}", zone)).ToList()
                };

                if (string.IsNullOrEmpty(clusterDns.ZoneName))
                {
                    clusterDns.ZoneName = clusterDns.ControlPlane.FirstOrDefault()?.ZoneName;
                }

                return new ServiceResult<ClusterDns>()
                {
                    Success = true,
                    Data = clusterDns
                };
            }
            catch (ApiException ex)
            {
                return new ServiceResult<ClusterDns>
                {
                    Errors = new List<string>
                    {
                        ex.Message
                    },
                    Success = false
                };
            }

        }

        [HttpPut("{name}")]
        [Produces(typeof(ServiceResult<ClusterDns>))]
        public async Task<ActionResult<ServiceResult<ClusterDns>>> Put([FromRoute] string name, [FromBody] ClusterDns incomingCluster)
        {
            _logger.LogTrace("Processing request to update ClusterDns Record");
            try
            {
                var existingRecordResult = await Get(incomingCluster.Name, incomingCluster.ZoneName);

                if (existingRecordResult is not { Value.Success: true })
                {
                    return new ServiceResult<ClusterDns>
                    {
                        Success = false,
                        Errors = new List<string> { $"Cluster {name} does not exist." }
                    };
                }

                var existingRecord = existingRecordResult.Value.Data;

                var success = true;
                var recordsToCreate = new List<DnsRecord>();
                var recordsToRemove = new List<DnsRecord>();

                // Incoming request has a control plane record not in the existing record - add new
                foreach (var newControlPlane in incomingCluster.ControlPlane)
                {
                    if (!existingRecord.ControlPlane.Exists(rec =>
                            rec.HostName == newControlPlane.HostName && rec.Data == newControlPlane.Data &&
                            rec.ZoneName == newControlPlane.ZoneName && rec.RecordType == newControlPlane.RecordType))
                    {
                        recordsToCreate.Add(newControlPlane);
                    }
                }

                // Existing request has a control plane record not in the new one - delete existing
                foreach (var existingControlPlane in existingRecord.ControlPlane)
                {
                    if (!incomingCluster.ControlPlane.Exists(rec =>
                            rec.HostName == existingControlPlane.HostName && rec.Data == existingControlPlane.Data &&
                            rec.ZoneName == existingControlPlane.ZoneName && rec.RecordType == existingControlPlane.RecordType))
                    {
                        recordsToRemove.Add(existingControlPlane);
                    }
                }

                // Incoming request has a traffic record not in the existing record - add new
                foreach (var newTraffic in incomingCluster.Traffic)
                {
                    if (!existingRecord.Traffic.Exists(rec =>
                            rec.HostName == newTraffic.HostName && rec.Data == newTraffic.Data &&
                            rec.ZoneName == newTraffic.ZoneName && rec.RecordType == newTraffic.RecordType))
                    {
                        recordsToCreate.Add(newTraffic);
                    }
                }

                // Existing request has a traffic record not in the new one - delete existing
                foreach (var existingTraffic in existingRecord.Traffic)
                {
                    if (!incomingCluster.Traffic.Exists(rec =>
                            rec.HostName == existingTraffic.HostName && rec.Data == existingTraffic.Data &&
                            rec.ZoneName == existingTraffic.ZoneName && rec.RecordType == existingTraffic.RecordType))
                    {
                        recordsToRemove.Add(existingTraffic);
                    }
                }

                if (recordsToCreate.Count > 0)
                {
                    success = await DnsService.BulkCreateDnsRecords(recordsToCreate);
                }

                foreach (var recordToDelete in recordsToRemove)
                {
                    if (!await DnsService.DeleteDnsRecord(recordToDelete))
                    {
                        success = false;
                    }
                }

                return !success
                    ? new ServiceResult<ClusterDns>
                    {
                        Success = false
                    }
                    : await Get(incomingCluster.Name, incomingCluster.ZoneName);
            }
            catch (ApiException ex)
            {
                return new ServiceResult<ClusterDns>
                {
                    Success = false,
                    Errors = new List<string>
                    {
                        ex.Message
                    }
                };
            }
        }

        [HttpPost]
        public async Task<ActionResult<ServiceResult<ClusterDns>>> Post([FromBody] NewClusterRequest newRequest)
        {
            _logger.LogTrace("Processing request to create new ClusterDns Record");
            try
            {
                var controlPlaneHost = $"cp-{newRequest.Name}";
                var trafficHost = $"tfx-{newRequest.Name}";
                var success = true;
                var recordsToCreate = new List<DnsRecord>();
                foreach (var serverIp in newRequest.ControlPlaneIps)
                {
                    recordsToCreate.Add(new DnsRecord
                    {
                        HostName = controlPlaneHost,
                        Data = serverIp,
                        RecordType = DnsRecordType.A,
                        ZoneName = newRequest.ZoneName
                    });
                }

                foreach (var trafficIp in newRequest.TrafficIps)
                {
                    recordsToCreate.Add(new DnsRecord
                    {
                        HostName = trafficHost,
                        Data = trafficIp,
                        RecordType = DnsRecordType.A,
                        ZoneName = newRequest.ZoneName
                    });
                }

                success = await DnsService.BulkCreateDnsRecords(recordsToCreate);

                return !success
                    ? new ServiceResult<ClusterDns>
                    {
                        Success = false
                    }
                    : await Get(newRequest.Name, newRequest.ZoneName);
            }
            catch (ApiException ex)
            {
                return new ServiceResult<ClusterDns>
                {
                    Success = false,
                    Errors = new List<string>
                    {
                        ex.Message
                    }
                };
            }
        }
    }
}
