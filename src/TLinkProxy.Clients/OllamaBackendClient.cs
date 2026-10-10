using TLinkProxy.Abstractions;
using TLinkProxy.Base;

namespace TLinkProxy.Clients
{
    public class OllamaBackendClient : HttpBackendClientBase
    {
        public OllamaBackendClient(IHttpClientFactory factory, IProxySessionLogger logger)
            : base(factory, logger)
        {
        }

        protected override string ClientLabel => Constants.OllamaBackendLabel;
    }
}
