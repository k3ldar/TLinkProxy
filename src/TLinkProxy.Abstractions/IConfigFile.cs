namespace TLinkProxy.Abstractions
{
    public interface IConfigFile
    {
        /// <summary>Reads the current configuration from disk.</summary>
        IAiSettings GetConfig();

        /// <summary>Writes the supplied configuration to disk and returns whether it succeeded.</summary>
        bool SaveConfig(IAiSettings config);

        /// <summary>
        /// Raised when the configuration file has changed on disk, indicating that the application should reload its configuration.
        /// </summary>
        event EventHandler ConfigChanged;
    }
}