using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IOpenAiRequestParser
    {
        /// <summary>
        /// Parse a raw HTTP request body (JSON) into an OpenAIModel and optional RoutingEnvelope.
        /// Returns (null, null, normalizedBody) on parse failure.
        /// </summary>
        Task<(OpenAIModel Model, RoutingEnvelope Routing, string NormalizedBody)> ParseAsync(string body, string contentType);
    }
}
