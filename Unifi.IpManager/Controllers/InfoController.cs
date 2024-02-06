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

namespace Unifi.IpManager.Controllers
{
    /// <summary>
    /// Class InfoController.
    /// Implements the <see cref="ControllerBase" />
    /// </summary>
    /// <seealso cref="ControllerBase" />
    /// <remarks>
    /// Initializes a new instance of the <see cref="InfoController"/> class.
    /// </remarks>
    /// <param name="log"></param>
    /// <param name="myOpts">The options.</param>
    [ApiVersion("1.0")]
    [Route("[controller]")]
    [Authorize()]
    [ApiController]
    public class InfoController(ILogger<InfoController> log, IOptions<UnifiControllerOptions> myOpts) : ControllerBase
    {
        private ILogger<InfoController> Log { get; set; } = log;
        /// <summary>
        /// Gets or sets the UnifiControllerOptions options.
        /// </summary>
        /// <value>The UnifiControllerOptions options.</value>
        private UnifiControllerOptions UnifiControllerOptions { get; set; } = myOpts.Value;

        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>ActionResult&lt;Models.Info&gt;.</returns>
        [HttpGet]
        public ActionResult<Info> Get()
        {
            var info = new Info
            {
                UnifiControllerOptions = UnifiControllerOptions,
                Version = GetVersion()
            };

            return info;
        }

        private string GetVersion()
        {
            try
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

                return fileInfo.ProductVersion;
            }
            catch (Exception e)
            {
                Log.LogError(e, "Error retrieving file version");
                return "0.0.0.0";
            }
        }
    }
}
