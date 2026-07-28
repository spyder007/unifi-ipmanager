using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using Unifi.IpManager.Services;

namespace Unifi.IpManager.Controllers;

/// <summary>
/// Class LoggingController with Options.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientController"/> class.
/// </remarks>
/// <param name="unifiDnsService">The unifi service.</param>
[ApiVersion("1.0")]
[Route("[controller]")]
[ApiController]
[Authorize]
public class DnsController(IUnifiDnsService unifiDnsService) : ControllerBase
{
    /// <summary>
    /// Gets or sets the options.
    /// </summary>
    /// <value>The options.</value>
    private IUnifiDnsService IUnifiDnsService { get; set; } = unifiDnsService;

    /// <summary>
    /// Gets this instance.
    /// </summary>
    /// <returns>ActionResult&lt;System.String&gt;.</returns>
    [HttpGet]
    public async Task<ActionResult<ServiceResult<List<HostDnsRecord>>>> Get()
    {
        return await IUnifiDnsService.GetHostDnsRecords();
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<HostDnsRecord>>> Post([FromBody] HostDnsRecord hostRecord)
    {
        return await IUnifiDnsService.CreateHostDnsRecord(hostRecord);
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<ActionResult<ServiceResult<HostDnsRecord>>> Put([FromRoute] string id, [FromBody] HostDnsRecord hostRecord)
    {
        hostRecord.Id = id;
        return await IUnifiDnsService.UpdateDnsHostRecord(hostRecord);
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<ActionResult<ServiceResult>> DeleteClient([FromRoute] string id)
    {
        return await IUnifiDnsService.DeleteHostDnsRecord(id);
    }
}
