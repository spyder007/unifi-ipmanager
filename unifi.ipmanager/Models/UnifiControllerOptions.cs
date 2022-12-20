namespace unifi.ipmanager.Models
{
    /// <summary>
    /// Class MyOptions.
    /// </summary>
    public class UnifiControllerOptions
    {
        /// <summary>
        /// Unifi Controller URL
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>The client identifier.</value>
        public string Username { get; set; }
        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        /// <value>The client secret.</value>
        public string Password { get; set; }

    }

}
