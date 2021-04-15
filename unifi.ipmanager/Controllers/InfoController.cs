using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using unifi.ipmanager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace unifi.ipmanager.Controllers
{
    /// <summary>
    /// Class InfoController.
    /// Implements the <see cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [ApiVersion("1.0")]
    [Route("[controller]")]
    [Authorize()]
    [ApiController]
    public class InfoController : ControllerBase
    {

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        private IConfiguration Configuration { get; set; }
        /// <summary>
        /// Gets or sets the UnifiControllerOptions options.
        /// </summary>
        /// <value>The UnifiControllerOptions options.</value>
        private UnifiControllerOptions UnifiControllerOptions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfoController"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="myOpts">The options.</param>
        public InfoController(IConfiguration configuration, IOptions<UnifiControllerOptions> myOpts)
        {
            Configuration = configuration;
            UnifiControllerOptions = myOpts.Value;
        }

        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>ActionResult&lt;Models.Info&gt;.</returns>
        [HttpGet]
        public ActionResult<Info> Get()
        {
            var info = new Info {UnifiControllerOptions = UnifiControllerOptions};


            return info;
        }
    }
}
