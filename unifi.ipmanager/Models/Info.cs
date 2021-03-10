using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unifi.ipmanager.Models
{
    /// <summary>
    /// Class Info.
    /// </summary>
    public class Info
    {
        /// <summary>
        /// Gets or sets the cache database connection string.
        /// </summary>
        /// <value>The cache database connection string.</value>
        public string CacheDbConnectionString { get; set; }
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public UnifiControllerOptions UnifiControllerOptions { get; set; }
    }
}
