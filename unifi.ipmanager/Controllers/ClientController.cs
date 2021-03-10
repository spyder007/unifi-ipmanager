using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using unifi.ipmanager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private ILogger<ClientController> _logger;

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
        public async Task<ActionResult<List<UniClient>>> Get()
        {
            return await IUnifyService.GetAllFixedClients();
        }

        [HttpPost]
        [Route("provision")]
        public async Task<ActionResult<UniClient>> ProvisionClient([FromBody] ProvisionRequest request)
        {
            return await IUnifyService.ProvisionNewClient(request.Group, request.Name, request.HostName, request.Static_ip);
        }
    }
}
