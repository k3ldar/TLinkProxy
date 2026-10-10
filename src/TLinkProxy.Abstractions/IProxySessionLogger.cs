namespace TLinkProxy.Abstractions
{
    public interface IProxySessionLogger
    {
        Task LogAsync(string sessionId, string message);

        Task LogBytesAsync(string sessionId, byte[] bytes, string label = null);
    }
}
