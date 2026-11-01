using Microsoft.Extensions.Hosting;

namespace TLinkProxy.Abstractions
{
    /// <summary>
    /// Background sampler that periodically captures host memory and thread-pool statistics and
    /// exposes the most recent snapshot for rendering on the dashboard.
    /// </summary>
    public interface IProcessDiagnostics : IHostedService, IDisposable
    {
        /// <summary>Most recent diagnostics snapshot (never null).</summary>
        ProcessDiagnosticsSnapshot Snapshot { get; }

        /// <summary>Currently effective sampling interval.</summary>
        TimeSpan Interval { get; }
    }
}
