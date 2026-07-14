using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
/// <param name="logger">The logger.</param>
[ApiVersion("1.0")]
[Route("[controller]")]
[ApiController]
[Authorize]
public class DnsController(IUnifiDnsService unifiDnsService, ILogger<DnsController> logger) : ControllerBase
{
    private readonly ILogger<DnsController> _logger = logger;

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
        _logger.LogTrace("Processing request for all DNS records");
        return await IUnifiDnsService.GetHostDnsRecords();
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<HostDnsRecord>>> Post([FromBody] HostDnsRecord hostRecord)
    {
        _logger.LogTrace("Processing request for new Dns Record");
        return await IUnifiDnsService.CreateHostDnsRecord(hostRecord);
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<ActionResult<ServiceResult<HostDnsRecord>>> Put([FromRoute] string id, [FromBody] HostDnsRecord hostRecord)
    {
        _logger.LogTrace("Processing request for update dns record");
        hostRecord.Id = id;
        return await IUnifiDnsService.UpdateDnsHostRecord(hostRecord);
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<ActionResult<ServiceResult>> DeleteClient([FromRoute] string id)
    {
        _logger.LogTrace("Processing request for delete dns record");
        return await IUnifiDnsService.DeleteHostDnsRecord(id);
    }
}
