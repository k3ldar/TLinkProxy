using TLinkProxy.Abstractions;
using TLinkProxy.Base;

namespace TLinkProxy.Clients
{
    /// <summary>
    /// Adapter for local llama.cpp server instances (OpenAI-compatible endpoint, no auth).
    /// </summary>
    public class LlamaCppBackendClient : HttpBackendClientBase
    {
        public LlamaCppBackendClient(IHttpClientFactory factory, IProxySessionLogger logger)
            : base(factory, logger)
        {
        }

        protected override string ClientLabel => Constants.LlamaCppBackendLabel;
    }
}
