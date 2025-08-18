using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Unifi.IpManager.Models.Dns;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Services;
using Unifi.IpManager.ExternalServices;
using Asp.Versioning;

namespace Unifi.IpManager.Controllers;

/// <summary>
/// Initializes a new instance of the <see cref="ClientController"/> class.
/// </summary>
/// <param name="clusterDnsService">The IClusterDnsService for this request.</param>
[ApiVersion("1.0")]
[Route("[controller]")]
[ApiController]
[Authorize]
public class ClusterDnsController(IClusterDnsService clusterDnsService)
{
    private readonly IClusterDnsService ClusterDnsService = clusterDnsService;

    /// <summary>
    /// Gets this instance.
    /// </summary>
    /// <returns>ActionResult&lt;System.String&gt;.</returns>
    [HttpGet("{name}")]
    public async Task<ActionResult<ServiceResult<ClusterDns>>> Get([FromRoute] string name, [FromQuery] string zone)
    {
        return await ClusterDnsService.GetClusterDns(name, zone);
    }

    [HttpPut("{name}")]
    [Produces(typeof(ServiceResult<ClusterDns>))]
    public async Task<ActionResult<ServiceResult<ClusterDns>>> Put([FromRoute] string name, [FromBody] ClusterDns incomingCluster)
    {
       incomingCluster.Name = name;
       return await ClusterDnsService.UpdateClusterDns(incomingCluster);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<ClusterDns>>> Post([FromBody] NewClusterRequest newRequest)
    {
        return await ClusterDnsService.CreateClusterDns(newRequest);
    }
}
