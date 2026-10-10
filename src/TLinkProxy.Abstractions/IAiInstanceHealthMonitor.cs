using Microsoft.Extensions.Hosting;

namespace TLinkProxy.Abstractions
{
    public interface IAiInstanceHealthMonitor : IHostedService, IDisposable
    {
        // Intentionally left empty
    }
}
