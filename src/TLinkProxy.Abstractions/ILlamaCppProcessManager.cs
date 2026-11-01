using Microsoft.Extensions.Hosting;

namespace TLinkProxy.Abstractions
{
    public interface ILlamaCppProcessManager : IHostedService, IDisposable
    {
        Task EnsureStartedAsync(IAiModelConfig modelConfig, CancellationToken ct);
        DateTime? GetLastActivityUtc(string alias);

        /// <summary>Returns memory usage for each currently managed child process.</summary>
        IReadOnlyList<ProcessMemorySample> GetProcessMemory();
    }
}