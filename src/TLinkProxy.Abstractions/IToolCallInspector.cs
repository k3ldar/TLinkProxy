namespace TLinkProxy.Abstractions
{
    public interface IToolCallInspector
    {
        Task<string> InspectResponseAsync(string responseBody, string sessionId, string fallbackModel, bool isStreaming, CancellationToken cancellationToken);
    }
}
