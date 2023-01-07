using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using unifi.ipmanager.Models.DTO;
using unifi.ipmanager.Models.Unifi;
using unifi.ipmanager.Services;

namespace unifi.ipmanager.Controllers
{
    /// <summary>
    /// Class LoggingController with Options.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class ClientController : ControllerBase
    {
        private readonly ILogger<ClientController> _logger;

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        private IUnifiService IUnifyService { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientController"/> class.
        /// </summary>
        /// <param name="unifiService">The unifi service.</param>
        /// <param name="logger">The logger.</param>
        public ClientController(IUnifiService unifiService, ILogger<ClientController> logger)
        {
            IUnifyService = unifiService;
            _logger = logger;
        }

        // GET api/values/5
        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>ActionResult&lt;System.String&gt;.</returns>
        [HttpGet]
        public async Task<ActionResult<ServiceResult<List<UniClient>>>> Get()
        {
            _logger.LogInformation("Processing client list request.");
            return await IUnifyService.GetAllFixedClients();
        }

        [HttpPost]
        public async Task<ActionResult<ServiceResult<UniClient>>> Post([FromBody] NewClientRequest newRequest)
        {
            _logger.LogInformation("Processing create client request.");
            return await IUnifyService.CreateClient(newRequest);
        }

        [HttpPut]
        [Route("{mac}")]
        public async Task<ActionResult<ServiceResult>> Put([FromRoute] string mac, [FromBody] EditClientRequest editRequest)
        {
            _logger.LogInformation("Processing client update request.");
            return await IUnifyService.UpdateClient(mac, editRequest);
        }


        [HttpDelete]
        [Route("{mac}")]
        public async Task<ActionResult<ServiceResult>> DeleteClient([FromRoute] string mac)
        {
            _logger.LogInformation("Processing client delete request.");
            return await IUnifyService.DeleteClient(mac);
        }

        [HttpPost]
        [Route("provision")]
        public async Task<ActionResult<ServiceResult<UniClient>>> ProvisionClient([FromBody] ProvisionRequest request)
        {
            _logger.LogInformation("Processing provision client request.");
            return await IUnifyService.ProvisionNewClient(request.Group, request.Name, request.HostName, request.StaticIp, request.SyncDns);
        }
    }
}
