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
/// <param name="unifiService">The unifi service.</param>
[ApiVersion("1.0")]
[Route("[controller]")]
[ApiController]
[Authorize]
public class ClientController(IUnifiService unifiService) : ControllerBase
{
    /// <summary>
    /// Gets or sets the options.
    /// </summary>
    /// <value>The options.</value>
    private IUnifiService IUnifyService { get; set; } = unifiService;

    /// <summary>
    /// Gets this instance.
    /// </summary>
    /// <returns>ActionResult&lt;System.String&gt;.</returns>
    [HttpGet]
    public async Task<ActionResult<ServiceResult<List<UniClient>>>> Get()
    {
        return await IUnifyService.GetAllFixedClients();
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResult<UniClient>>> Post([FromBody] NewClientRequest newRequest)
    {
        return await IUnifyService.CreateClient(newRequest);
    }

    [HttpPut]
    [Route("{mac}")]
    public async Task<ActionResult<ServiceResult>> Put([FromRoute] string mac, [FromBody] EditClientRequest editRequest)
    {
        return await IUnifyService.UpdateClient(mac, editRequest);
    }


    [HttpDelete]
    [Route("{mac}")]
    public async Task<ActionResult<ServiceResult>> DeleteClient([FromRoute] string mac)
    {
        return await IUnifyService.DeleteClient(mac);
    }

    [HttpPost]
    [Route("provision")]
    public async Task<ActionResult<ServiceResult<UniClient>>> ProvisionClient([FromBody] ProvisionRequest request)
    {
        return await IUnifyService.ProvisionNewClient(request);
    }
}
