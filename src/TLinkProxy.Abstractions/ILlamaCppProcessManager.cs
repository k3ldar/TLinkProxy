using Microsoft.Extensions.Hosting;

namespace TLinkProxy.Abstractions
{
    public interface ILlamaCppProcessManager : IHostedService, IDisposable
    {
        Task EnsureStartedAsync(IAiModelConfig modelConfig, CancellationToken ct);
        DateTime? GetLastActivityUtc(string alias);
    }
}