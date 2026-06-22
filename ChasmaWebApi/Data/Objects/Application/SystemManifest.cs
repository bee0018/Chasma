namespace ChasmaWebApi.Data.Objects.Application
{
    /// <summary>
    /// Class representing the system manifest recieved from the remote host.
    /// </summary>
    public class SystemManifest
    {
        /// <summary>
        /// Gets or sets the version of the software application.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this system manifests signifies a critical update.
        /// </summary>
        public bool CriticalUpdate { get; set; }

        /// <summary>
        /// Gets or sets the download url for the windows version of Emryce.
        /// </summary>
        public string WindowsDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the download url for the linux version of Emryce.
        /// </summary>
        public string LinuxDownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the checksum of the Windows download artifact file.
        /// </summary>
        public string WindowsChecksum { get; set; }

        /// <summary>
        /// Gets or sets the checksum of the Linux download artifact file.
        /// </summary>
        public string LinuxChecksum { get; set; }

        /// <summary>
        /// Gets or sets the list of changes in the change log.
        /// </summary>
        public List<string> ChangeLog { get; set; } = [];
    }
}
