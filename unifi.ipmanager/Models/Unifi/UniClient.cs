using Newtonsoft.Json;

namespace unifi.ipmanager.Models.Unifi
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
        public string _id { get; set; }

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
        public bool Use_fixedip { get; set; }

        /// <summary>
        /// Gets or sets the fixed ip.
        /// </summary>
        /// <value>The fixed ip.</value>
        public string Fixed_ip { get; set; }

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
        public UniNote NoteObject
        {
            get
            {
                if (_note != null) return _note;

                if (Noted && !string.IsNullOrWhiteSpace(Note))
                {
                    _note = JsonConvert.DeserializeObject<UniNote>(Note);
                }
                else
                {
                    _note = new UniNote();
                }

                return _note;
            }
        }
    }
}
