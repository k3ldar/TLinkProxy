using TLinkProxy.Abstractions;
using TLinkProxy.Kernel;

namespace TLinkProxy.Base.Models
{
    public class OptimizationResult : IOptimizationResult
    {
        public string Body { get; set; }

        public RoutingEnvelope Routing { get; set; }

        public string SessionId { get; set; }

        public OpenAIModel RequestModel { get; set; }

        public List<string> Optimizations { get; } = [];
    }
}
