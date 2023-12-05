using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Unifi.IpManager.Models.Unifi
{
    /// <summary>
    /// Class UniClient.
    /// </summary>
    public class UniClient
    {
        private UniNote _note;

        public UniClient()
        {
            ObjectType = "client";
        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [JsonProperty("_id")]
        [JsonPropertyName("_id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the mac.
        /// </summary>
        /// <value>The mac.</value>
        public string Mac { get; set; }

        /// <summary>
        /// Gets or sets the hostname.
        /// </summary>
        /// <value>The hostname.</value>
        public string Hostname { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use fixedip].
        /// </summary>
        /// <value><c>true</c> if [use fixedip]; otherwise, <c>false</c>.</value>
        [JsonProperty("use_fixedip")]
        [JsonPropertyName("use_fixedip")]
        public bool UseFixedIp { get; set; }

        /// <summary>
        /// Gets or sets the fixed ip.
        /// </summary>
        /// <value>The fixed ip.</value>
        [JsonProperty("fixed_ip")]
        [JsonPropertyName("fixed_ip")]
        public string FixedIp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UniClient"/> is noted.
        /// </summary>
        /// <value><c>true</c> if noted; otherwise, <c>false</c>.</value>
        public bool Noted { get; set; }
        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        /// <value>The note.</value>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        /// <value>The type of the object.</value>
        public string ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the IPBlock Name associated with this IP
        /// </summary>
        public string IpGroup { get; set; }

        public UniNote Notes
        {
            get
            {
                if (_note != null)
                {
                    return _note;
                }

                _note = Noted && !string.IsNullOrWhiteSpace(Note) ? JsonConvert.DeserializeObject<UniNote>(Note) : new UniNote();

                return _note;
            }
        }
    }
}
