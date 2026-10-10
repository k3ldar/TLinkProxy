using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IOptimizationResult
    {
        string Body { get; set; }
        OpenAIModel RequestModel { get; set; }
        RoutingEnvelope Routing { get; set; }
        string SessionId { get; set; }

        List<string> Optimizations { get; }
    }
}
