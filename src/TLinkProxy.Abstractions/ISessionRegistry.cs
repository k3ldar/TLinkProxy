namespace TLinkProxy.Abstractions
{
    /// <summary>
    /// Authoritative registry that maps a client identity to a stable, proxy-issued session id.
    /// </summary>
    public interface ISessionRegistry
    {
        string GetOrCreateSessionId(string explicitSessionId, string apiKey, string clientIp, string userAgent);
    }
}
