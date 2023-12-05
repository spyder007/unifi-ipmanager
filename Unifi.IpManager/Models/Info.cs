using Unifi.IpManager.Options;

namespace Unifi.IpManager.Models
{
    /// <summary>
    /// Class Info.
    /// </summary>
    public class Info
    {
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public UnifiControllerOptions UnifiControllerOptions { get; set; }

        public string Version { get; set; }


    }
}
