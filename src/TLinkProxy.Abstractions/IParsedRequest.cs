using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IParsedRequest
    {
        string ContentType { get; }
        string SessionId { get; }
        string Body { get; set; }
        List<string> Changes { get; }
        OpenAIModel RequestModel { get; }
        RoutingEnvelope Routing { get; }
    }
}
