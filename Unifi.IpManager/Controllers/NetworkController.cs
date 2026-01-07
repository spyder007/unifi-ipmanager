using System;
using System.Diagnostics;
using System.Reflection;
using Unifi.IpManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Unifi.IpManager.Options;
using Asp.Versioning;
using Unifi.IpManager.Models.Unifi;
using Unifi.IpManager.Services;
using Unifi.IpManager.Models.DTO;
using System.Threading.Tasks;

namespace Unifi.IpManager.Controllers;

/// <summary>
/// Class InfoController.
/// Implements the <see cref="ControllerBase" />
/// </summary>
/// <seealso cref="ControllerBase" />
/// <remarks>
/// Initializes a new instance of the <see cref="InfoController"/> class.
/// </remarks>
/// <param name="log"></param>
/// <param name="unifiService">The IUnifiService</param>
[ApiVersion("1.0")]
[Route("[controller]")]
[Authorize()]
[ApiController]
public class NetworkController(ILogger<NetworkController> log, IUnifiService unifiService) : ControllerBase
{
    private IUnifiService UnifiService { get; } = unifiService;

    /// <summary>
    /// Gets this instance.
    /// </summary>
    /// <returns>ActionResult&lt;Models.Info&gt;.</returns>
    [HttpGet]
    [Route("{name}")]
    public async Task<ActionResult<ServiceResult<UnifiNetwork>>> GetByName(string name)
    {
        log.LogTrace("Processing request for network by name: {NetworkName}", name);
        return await UnifiService.GetNetworkByName(name);
    }
}
