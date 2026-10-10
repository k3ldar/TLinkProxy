namespace TLinkProxy.Abstractions
{
    public interface IBackendClient
    {
        Task<IBackendResponse> SendAsync(IAiModelConfig modelConfig, string pathAndQuery, string bodyJson, string sessionId, CancellationToken ct);
    }
}
