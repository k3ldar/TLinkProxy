namespace TLinkProxyConsole
{
    internal static class ConsoleConstants
    {
        /// <summary>
        /// Base URL of the TLinkProxy proxy instance to test against.
        /// Change this constant (or override per-fixture via the "url" field) to
        /// point the runner at a different host/port before building.
        /// </summary>
        internal const string DefaultServerUrl = "http://localhost:11430";
    }
}
