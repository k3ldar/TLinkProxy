using TLinkProxy.Kernel;

namespace TLinkProxy.Abstractions
{
    public interface IOpenAiRequestParser
    {
        /// <summary>
        /// Parse a raw HTTP request body (JSON) into an OpenAIModel and optional RoutingEnvelope.
        /// Returns (null, null, normalizedBody) when the body is malformed. When the full model cannot
        /// be deserialized but the top-level model name is recoverable, a stub model carrying that name
        /// is returned so backend selection still routes to the requested model.
        /// </summary>
        Task<(OpenAIModel Model, RoutingEnvelope Routing, string NormalizedBody)> ParseAsync(string body, string contentType);
    }
}
