namespace Unifi.IpManager.Options
{
    /// <summary>
    /// Class MyOptions.
    /// </summary>
    public class UnifiControllerOptions
    {
        public const string SectionName = "UnifiControllerOptions";
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


        /// <summary>
        /// Gets or sets a value indicating whether this instance is on Unifi OS.
        /// </summary>
        /// <remarks>If the controller software is hosted on Unifi OS, some changes
        /// are requied for paths and token handling.</remarks>
        /// <value><c>true</c> if this instance is on Unifi OS; otherwise, <c>false</c>.</value>
        public bool IsUnifiOs { get; set; } = true;

        public string Site { get; set; } = "default";
    }

}
