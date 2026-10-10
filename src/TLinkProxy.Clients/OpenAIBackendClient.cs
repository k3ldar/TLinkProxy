using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using System.Net.Http.Headers;

namespace TLinkProxy.Clients
{
    public class OpenAIBackendClient : HttpBackendClientBase
    {
        public OpenAIBackendClient(IHttpClientFactory factory, IProxySessionLogger logger)
            : base(factory, logger)
        {
        }

        protected override string ClientLabel => Constants.OpenAIBackendLabel;

        protected override void ConfigureRequest(IAiModelConfig modelConfig, HttpRequestMessage request)
        {
            if (!String.IsNullOrWhiteSpace(modelConfig.ApiKeyEnvVar))
            {
                string key = Environment.GetEnvironmentVariable(modelConfig.ApiKeyEnvVar);

                if (!String.IsNullOrWhiteSpace(key))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue(Constants.AuthenticationSchemeBearer, key);
                }
            }
        }
    }
}
