using Newtonsoft.Json;
using unifi.ipmanager.Models.Unifi;

namespace unifi.ipmanager.Models.DTO
{
    public class EditClientRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public string Hostname { get; set; }

        public UniNote Notes { get; set; }
    }
}
